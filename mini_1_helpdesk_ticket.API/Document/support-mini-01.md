# Support Mini 01 — Technical Playbook cho Helpdesk Ticket API

Tài liệu này là phần bổ sung chuyên sâu cho:

- `independent-mini-projects.md`
- `mini-1-helpdesk-ticket-api.md`

Hai tài liệu trên trả lời **xây sản phẩm gì** và **API contract ra sao**. File này tập trung vào **triển khai như thế nào, vì sao chọn cách đó, test điều gì và học được kỹ năng nào**.

> Mục tiêu không phải làm CRUD chạy được. Mục tiêu là làm một CRUD nhỏ nhưng có tính đúng đắn khi nhiều request chạy đồng thời, có database bảo vệ invariant, query hiệu quả và lỗi đủ rõ để vận hành.

---

## 1. Mental model trước khi viết code

Mini 01 có một aggregate chính là `Ticket`:

```text
Ticket (aggregate root)
├── TicketComment[]
└── TicketLabel[] ──> Label
```

- `Ticket` sở hữu vòng đời của comment và liên kết label.
- Xóa ticket thì comment và `ticket_labels` bị xóa theo.
- `Label` có vòng đời độc lập; xóa ticket không được xóa label.
- Client chỉ thao tác aggregate qua use case, không ghi trực tiếp navigation tùy ý.

Luồng phụ thuộc nên giữ một chiều:

```text
HTTP Request
    ↓
Controller: binding + HTTP status
    ↓
Application Service: use case + business rule
    ↓
EF Core DbContext: unit of work + query
    ↓
PostgreSQL: constraint + atomicity + concurrency
```

Mỗi tầng có câu hỏi riêng:

| Tầng | Câu hỏi phải trả lời |
|---|---|
| Controller | Route nào, request nào, status code nào? |
| Service | Nghiệp vụ nào được phép, transaction boundary ở đâu? |
| EF configuration | Entity được lưu thành schema nào? |
| PostgreSQL | Nếu application có bug hoặc hai request đua nhau thì dữ liệu có còn đúng không? |
| Tests | Làm sao chứng minh invariant thật sự được bảo vệ? |

## 2. Cấu trúc solution nên dùng

```text
Mini01.Helpdesk/
├── src/
│   ├── Mini01.Helpdesk.Api/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Models/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── Mini01.Helpdesk.Application/
│   │   ├── Common/
│   │   ├── Labels/
│   │   └── Tickets/
│   └── Mini01.Helpdesk.Data/
│       ├── Configurations/
│       ├── Entities/
│       ├── Migrations/
│       └── HelpdeskDbContext.cs
└── tests/
    └── Mini01.Helpdesk.Tests/
```

Không tạo generic repository. EF Core `DbContext` đã cung cấp unit of work, change tracking và abstraction query. Thêm một lớp `Repository<T>` chỉ để gọi lại `Add`, `Find` và `SaveChanges` sẽ che mất khả năng projection, transaction và concurrency của EF Core.

## 3. Entity design

### 3.1. Enum

```csharp
public enum TicketPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public enum TicketStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4
}
```

Giữ giá trị số ổn định dù database lưu chuỗi. Không đổi tên enum tùy tiện sau khi đã có dữ liệu vì tên enum chính là persisted value.

### 3.2. `Ticket`

```csharp
public sealed class Ticket
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public string? AssigneeName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketLabel> TicketLabels { get; set; } = new List<TicketLabel>();
}
```

Không khai báo `RowVersion` như cột bình thường. PostgreSQL đã có system column `xmin`; configuration sẽ map một shadow property vào cột đó.

### 3.3. `TicketComment`

```csharp
public sealed class TicketComment
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string AuthorName { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
}
```

Comment trong MVP là append-only: tạo rồi không sửa. Thiết kế này giảm số use case và làm audit hội thoại dễ hiểu hơn.

### 3.4. `Label`

```csharp
public sealed class Label
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Color { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<TicketLabel> TicketLabels { get; set; } = new List<TicketLabel>();
}
```

### 3.5. `TicketLabel`

```csharp
public sealed class TicketLabel
{
    public Guid TicketId { get; set; }
    public Guid LabelId { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public Label Label { get; set; } = null!;
}
```

Join entity tường minh tốt hơn many-to-many ẩn vì:

- Nhìn schema và composite key rõ ràng.
- Dễ replace toàn bộ label.
- Sau này có thể thêm `AssignedAt` hoặc `AssignedBy` mà không đổi mô hình.

## 4. Fluent API configuration

Data Annotation phù hợp với request validation đơn giản. Schema quan trọng nên nằm ở `IEntityTypeConfiguration<T>` để nhìn thấy đầy đủ column type, constraint, index và delete behavior.

### 4.1. `TicketConfiguration`

```csharp
public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets", table =>
        {
            table.HasCheckConstraint(
                "ck_tickets_title_not_blank",
                "length(btrim(title)) > 0");

            table.HasCheckConstraint(
                "ck_tickets_description_not_blank",
                "length(btrim(description)) > 0");

            table.HasCheckConstraint(
                "ck_tickets_priority_valid",
                "priority IN ('Low', 'Medium', 'High', 'Urgent')");

            table.HasCheckConstraint(
                "ck_tickets_status_valid",
                "status IN ('Open', 'InProgress', 'Resolved', 'Closed')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TicketPriority.Medium)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TicketStatus.Open)
            .IsRequired();

        builder.Property(x => x.AssigneeName).HasMaxLength(150);

        builder.Property<uint>("xmin")
            .IsRowVersion()
            .HasColumnName("xmin")
            .HasColumnType("xid");

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("ux_tickets_code");

        builder.HasIndex(x => new { x.CreatedAt, x.Id })
            .IsDescending()
            .HasDatabaseName("ix_tickets_created_at_id");

        builder.HasIndex(x => new { x.Status, x.Priority })
            .HasDatabaseName("ix_tickets_status_priority");

        builder.HasIndex(x => x.AssigneeName)
            .HasDatabaseName("ix_tickets_assignee_name");
    }
}
```

Ba lớp bảo vệ không thay thế nhau:

1. Request validation tạo lỗi thân thiện.
2. Service validation bảo vệ business rule theo use case.
3. Database constraint bảo vệ mọi đường ghi và các race condition.

### 4.2. `TicketCommentConfiguration`

```csharp
public sealed class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("ticket_comments", table =>
        {
            table.HasCheckConstraint(
                "ck_ticket_comments_author_not_blank",
                "length(btrim(author_name)) > 0");

            table.HasCheckConstraint(
                "ck_ticket_comments_content_not_blank",
                "length(btrim(content)) > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.AuthorName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();

        builder.HasOne(x => x.Ticket)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TicketId, x.CreatedAt, x.Id })
            .HasDatabaseName("ix_ticket_comments_ticket_id_created_at");
    }
}
```

### 4.3. `LabelConfiguration`

```csharp
public sealed class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.ToTable("labels", table =>
        {
            table.HasCheckConstraint(
                "ck_labels_name_not_blank",
                "length(btrim(name)) > 0");

            table.HasCheckConstraint(
                "ck_labels_slug_format",
                "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");

            table.HasCheckConstraint(
                "ck_labels_color_hex",
                "color ~ '^#[0-9A-Fa-f]{6}$'");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(7).IsRequired();

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("ux_labels_slug");
    }
}
```

### 4.4. `TicketLabelConfiguration`

```csharp
public sealed class TicketLabelConfiguration : IEntityTypeConfiguration<TicketLabel>
{
    public void Configure(EntityTypeBuilder<TicketLabel> builder)
    {
        builder.ToTable("ticket_labels");
        builder.HasKey(x => new { x.TicketId, x.LabelId });

        builder.HasOne(x => x.Ticket)
            .WithMany(x => x.TicketLabels)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Label)
            .WithMany(x => x.TicketLabels)
            .HasForeignKey(x => x.LabelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

Composite primary key là lớp bảo vệ cuối cùng chống gắn một label hai lần vào cùng ticket.

## 5. `HelpdeskDbContext` và timestamp tập trung

```csharp
public sealed class HelpdeskDbContext : DbContext
{
    public HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options)
        : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<TicketLabel> TicketLabels => Set<TicketLabel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HelpdeskDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Ticket>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;

            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }

        foreach (var entry in ChangeTracker.Entries<TicketComment>())
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;

        foreach (var entry in ChangeTracker.Entries<Label>())
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
    }
}
```

Điểm đáng học từ `document_first.Repo/AppDbContext.cs` là timestamp được đóng dấu tập trung. Service không cần nhớ set `UpdatedAt` ở từng nhánh.

Khi project lớn hơn, thay các vòng lặp theo concrete type bằng `ICreationAuditable` và `IAuditableEntity`. Với Mini 01, cách explicit ở trên dễ đọc hơn.

## 6. Tự động sinh ticket code đúng khi concurrent

### 6.1. Vì sao không dùng `Count + 1`

Sai:

```csharp
var number = await db.Tickets.CountAsync(ct) + 1;
```

Hai request có thể cùng đọc count là 10 và cùng sinh `TCK-0011`. Xóa ticket còn làm code cũ bị tái sử dụng.

### 6.2. Dùng PostgreSQL sequence

Migration:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(
        "CREATE SEQUENCE ticket_code_seq START WITH 1 INCREMENT BY 1;");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP SEQUENCE ticket_code_seq;");
}
```

Generator:

```csharp
public sealed class TicketCodeGenerator
{
    private readonly HelpdeskDbContext _db;

    public TicketCodeGenerator(HelpdeskDbContext db) => _db = db;

    public async Task<string> NextAsync(CancellationToken ct)
    {
        var values = await _db.Database
            .SqlQueryRaw<long>(
                "SELECT nextval('ticket_code_seq') AS \"Value\"")
            .ToListAsync(ct);

        return $"TCK-{values.Single():D4}";
    }
}
```

`nextval` là atomic giữa nhiều request và nhiều instance của API. Sequence có thể có khoảng trống khi transaction rollback; đó không phải lỗi. Mã định danh cần unique và tăng dần, không cần liên tục tuyệt đối.

### 6.3. Điều cần nói được khi phỏng vấn

- `UNIQUE(code)` vẫn cần dù sequence đang sinh số.
- Sequence giải quyết race ở bước cấp số.
- Unique constraint bảo vệ dữ liệu nếu generator hoặc một đường import khác có bug.
- Không “sửa” gap bằng cách đọc max code; cách đó đưa race condition quay lại.

## 7. DTO và boundary của API

Không trả entity EF trực tiếp. DTO nên là immutable record:

```csharp
public sealed record CreateTicketRequest(
    [property: Required, MaxLength(200)] string Title,
    [property: Required] string Description,
    TicketPriority? Priority,
    [property: MaxLength(150)] string? AssigneeName);

public sealed record UpdateTicketRequest(
    [property: Required, MaxLength(200)] string Title,
    [property: Required] string Description,
    TicketPriority Priority,
    TicketStatus Status,
    [property: MaxLength(150)] string? AssigneeName,
    uint RowVersion);

public sealed record TicketListItemResponse(
    Guid Id,
    string Code,
    string Title,
    TicketPriority Priority,
    TicketStatus Status,
    string? AssigneeName,
    int CommentCount,
    IReadOnlyList<LabelResponse> Labels,
    uint RowVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
```

Entity, request và response tách nhau vì ba model thay đổi vì ba lý do khác nhau:

- Entity đổi khi schema lưu trữ đổi.
- Request đổi khi input contract đổi.
- Response đổi khi nhu cầu client đổi.

## 8. Service pattern cho từng use case

### 8.1. Create ticket

```csharp
public async Task<TicketDetailResponse> CreateAsync(
    CreateTicketRequest request,
    CancellationToken ct)
{
    var title = RequireText(request.Title, nameof(request.Title));
    var description = RequireText(request.Description, nameof(request.Description));

    var ticket = new Ticket
    {
        Id = Guid.NewGuid(),
        Code = await _codes.NextAsync(ct),
        Title = title,
        Description = description,
        Priority = request.Priority ?? TicketPriority.Medium,
        Status = TicketStatus.Open,
        AssigneeName = NullIfWhiteSpace(request.AssigneeName)
    };

    _db.Tickets.Add(ticket);
    await _db.SaveChangesAsync(ct);

    return await GetAsync(ticket.Id, ct);
}
```

Không nhận `Code`, `Status`, `CreatedAt` từ client lúc tạo. Đó là dữ liệu server sở hữu.

### 8.2. List/filter/paging bằng projection

```csharp
public async Task<PagedResponse<TicketListItemResponse>> ListAsync(
    TicketFilter filter,
    CancellationToken ct)
{
    var page = Math.Max(filter.Page, 1);
    var pageSize = Math.Clamp(filter.PageSize, 1, 100);

    IQueryable<Ticket> query = _db.Tickets.AsNoTracking();

    if (filter.Status is { } status)
        query = query.Where(x => x.Status == status);

    if (filter.Priority is { } priority)
        query = query.Where(x => x.Priority == priority);

    if (!string.IsNullOrWhiteSpace(filter.Assignee))
        query = query.Where(x =>
            x.AssigneeName != null &&
            EF.Functions.ILike(x.AssigneeName, $"%{filter.Assignee.Trim()}%"));

    if (!string.IsNullOrWhiteSpace(filter.Q))
    {
        var pattern = $"%{filter.Q.Trim()}%";
        query = query.Where(x =>
            EF.Functions.ILike(x.Code, pattern) ||
            EF.Functions.ILike(x.Title, pattern) ||
            EF.Functions.ILike(x.Description, pattern));
    }

    if (filter.LabelId is { } labelId)
        query = query.Where(x => x.TicketLabels.Any(tl => tl.LabelId == labelId));

    var totalCount = await query.CountAsync(ct);

    var items = await query
        .OrderByDescending(x => x.CreatedAt)
        .ThenByDescending(x => x.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new TicketListItemResponse(
            x.Id,
            x.Code,
            x.Title,
            x.Priority,
            x.Status,
            x.AssigneeName,
            x.Comments.Count(),
            x.TicketLabels
                .OrderBy(tl => tl.Label.Name)
                .Select(tl => new LabelResponse(
                    tl.Label.Id, tl.Label.Name, tl.Label.Slug, tl.Label.Color))
                .ToList(),
            EF.Property<uint>(x, "xmin"),
            x.CreatedAt,
            x.UpdatedAt))
        .ToListAsync(ct);

    return new(items, page, pageSize, totalCount);
}
```

Các điểm quan trọng:

- `AsNoTracking` giảm change-tracking cho read-only query.
- Chưa gọi `ToListAsync` cho đến khi filter, sort, paging và projection hoàn tất.
- `Comments.Count()` được dịch thành SQL; không load toàn bộ comment.
- Sort phụ theo `Id` làm thứ tự deterministic khi `CreatedAt` bằng nhau.
- Projection chỉ lấy cột response cần, không materialize aggregate lớn.

Trong môi trường production, log SQL hoặc dùng `ToQueryString()` ở test/debug để kiểm tra EF đã dịch query như mong muốn.

### 8.3. Update với optimistic concurrency

```csharp
public async Task<TicketDetailResponse> UpdateAsync(
    Guid id,
    UpdateTicketRequest request,
    CancellationToken ct)
{
    var ticket = await _db.Tickets
        .FirstOrDefaultAsync(x => x.Id == id, ct)
        ?? throw new NotFoundException("TICKET_NOT_FOUND", "Ticket không tồn tại.");

    _db.Entry(ticket)
        .Property("xmin")
        .OriginalValue = request.RowVersion;

    ticket.Title = RequireText(request.Title, nameof(request.Title));
    ticket.Description = RequireText(request.Description, nameof(request.Description));
    ticket.Priority = request.Priority;
    ticket.Status = request.Status;
    ticket.AssigneeName = NullIfWhiteSpace(request.AssigneeName);

    try
    {
        await _db.SaveChangesAsync(ct);
    }
    catch (DbUpdateConcurrencyException)
    {
        throw new ConflictException(
            "TICKET_CONCURRENCY_CONFLICT",
            "Ticket đã được người khác cập nhật. Hãy tải lại dữ liệu.");
    }

    return await GetAsync(id, ct);
}
```

SQL update về bản chất có điều kiện:

```sql
UPDATE tickets
SET ...
WHERE id = @id AND xmin = @old_xmin;
```

Nếu request khác đã ghi trước, `xmin` đổi, update khớp 0 hàng và EF ném `DbUpdateConcurrencyException`. Đây là cách đóng race condition giữa lúc đọc và lúc ghi.

Không dùng `Last-Write-Wins` cho màn hình hỗ trợ vì người lưu sau có thể vô tình xóa nội dung người lưu trước.

### 8.4. Thêm comment vào ticket chưa đóng

```csharp
public async Task<CommentResponse> AddCommentAsync(
    Guid ticketId,
    AddCommentRequest request,
    CancellationToken ct)
{
    var ticket = await _db.Tickets
        .FirstOrDefaultAsync(x => x.Id == ticketId, ct)
        ?? throw new NotFoundException("TICKET_NOT_FOUND", "Ticket không tồn tại.");

    if (ticket.Status == TicketStatus.Closed)
        throw new ConflictException("TICKET_CLOSED", "Ticket đã đóng.");

    var comment = new TicketComment
    {
        Id = Guid.NewGuid(),
        TicketId = ticket.Id,
        AuthorName = RequireText(request.AuthorName, nameof(request.AuthorName)),
        Content = RequireText(request.Content, nameof(request.Content))
    };

    _db.TicketComments.Add(comment);
    await _db.SaveChangesAsync(ct);

    return new(comment.Id, comment.TicketId, comment.AuthorName,
        comment.Content, comment.CreatedAt);
}
```

Stretch goal quan trọng: test race giữa `Close ticket` và `Add comment`. Check trong C# chưa tuyệt đối nếu hai transaction chạy đồng thời. Với scope Mini 01, optimistic concurrency và test hành vi tuần tự là đủ; khi muốn nâng cấp, cân nhắc transaction isolation/row lock hoặc đưa rule sang database trigger. Hãy ghi rõ trade-off thay vì giả vờ rule đã tuyệt đối.

### 8.5. Replace toàn bộ labels một cách nguyên tử

```csharp
public async Task<TicketLabelsResponse> ReplaceLabelsAsync(
    Guid ticketId,
    IReadOnlyCollection<Guid> labelIds,
    CancellationToken ct)
{
    if (labelIds.Count != labelIds.Distinct().Count())
        throw new BadRequestException("LABEL_IDS_INVALID", "Label ID bị trùng.");

    var ticket = await _db.Tickets
        .Include(x => x.TicketLabels)
        .FirstOrDefaultAsync(x => x.Id == ticketId, ct)
        ?? throw new NotFoundException("TICKET_NOT_FOUND", "Ticket không tồn tại.");

    var labels = await _db.Labels
        .Where(x => labelIds.Contains(x.Id))
        .OrderBy(x => x.Name)
        .ThenBy(x => x.Id)
        .ToListAsync(ct);

    if (labels.Count != labelIds.Count)
        throw new BadRequestException(
            "LABEL_IDS_INVALID",
            "Có label không tồn tại.");

    _db.TicketLabels.RemoveRange(ticket.TicketLabels);
    _db.TicketLabels.AddRange(labels.Select(label => new TicketLabel
    {
        TicketId = ticket.Id,
        LabelId = label.Id
    }));

    await _db.SaveChangesAsync(ct);

    return new(ticket.Id, labels.Select(MapLabel).ToList());
}
```

Chỉ gọi `SaveChangesAsync` một lần sau khi validate toàn bộ. EF Core mặc định bọc một lần save gồm nhiều statement trong transaction. Nếu insert thất bại, delete cũ cũng rollback.

## 9. Slug generation: tự động hóa nhỏ nhưng có giá trị

Tạo một pure function và test riêng:

```csharp
public static string ToSlug(string value)
{
    var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
    var chars = normalized
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        .ToArray();

    var ascii = new string(chars).Normalize(NormalizationForm.FormC);
    return Regex.Replace(ascii, "[^a-z0-9]+", "-").Trim('-');
}
```

Các case cần test:

| Input | Output |
|---|---|
| `Network` | `network` |
| `Mạng nội bộ` | `mang-noi-bo` |
| `  VPN lỗi  ` | `vpn-loi` |
| `C# / .NET` | `c-net` |

Sau khi sinh slug vẫn phải dựa vào `UNIQUE(slug)`. Hai request tạo cùng tên đồng thời có thể cùng qua check `AnyAsync`; chỉ database mới quyết định một request thắng.

Map PostgreSQL unique violation `23505` và constraint `ux_labels_slug` thành `409 LABEL_SLUG_DUPLICATED`. Không map mọi `DbUpdateException` thành duplicate slug vì lỗi FK hoặc NOT NULL sẽ bị che giấu.

## 10. Error contract và middleware

```csharp
public sealed record ErrorResponse(
    string Title,
    int Status,
    string Detail,
    string MessageCode,
    object? Errors,
    string TraceId,
    DateTimeOffset TimestampUtc);
```

Middleware nên:

- Log exception đầy đủ ở server.
- Trả message an toàn cho client.
- Map domain exception sang `400`, `404`, `409`.
- Map `DbUpdateConcurrencyException` sang `409`.
- Chỉ map đúng PostgreSQL constraint đã biết.
- Trả `traceId` để nối response với log.
- Không trả stack trace, SQL hay connection string.

`[ApiController]` mặc định trả `ValidationProblemDetails`. Hãy cấu hình `InvalidModelStateResponseFactory` để Data Annotation cũng dùng cùng `ErrorResponse`; frontend khi đó chỉ xử lý một error shape.

Phân biệt lỗi:

| Tình huống | HTTP | Message code |
|---|---:|---|
| Request field sai | 400 | `VALIDATION_FAILED` |
| Label IDs trùng/sai | 400 | `LABEL_IDS_INVALID` |
| Ticket không tồn tại | 404 | `TICKET_NOT_FOUND` |
| Ticket đã đóng | 409 | `TICKET_CLOSED` |
| `xmin` cũ | 409 | `TICKET_CONCURRENCY_CONFLICT` |
| Slug trùng | 409 | `LABEL_SLUG_DUPLICATED` |
| Lỗi không dự kiến | 500 | `INTERNAL_SERVER_ERROR` |

## 11. Controller mỏng

```csharp
[ApiController]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketService _tickets;

    public TicketsController(ITicketService tickets) => _tickets = tickets;

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTicketRequest request,
        CancellationToken ct)
    {
        var result = await _tickets.CreateAsync(request, ct);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponseFactory.Success(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(ApiResponseFactory.Success(
            await _tickets.GetAsync(id, ct),
            HttpContext.TraceIdentifier));
}
```

Controller không chứa LINQ query, không sinh code, không kiểm tra Closed, không catch `Exception`, không tự replace navigation.

## 12. Migration là source code, phải review

Lệnh:

```powershell
dotnet ef migrations add InitialHelpdeskSchema `
  --project src/Mini01.Helpdesk.Data `
  --startup-project src/Mini01.Helpdesk.Api

dotnet ef database update `
  --project src/Mini01.Helpdesk.Data `
  --startup-project src/Mini01.Helpdesk.Api
```

Sau khi generate migration, review:

- Có đúng bốn table nghiệp vụ.
- `ticket_labels` có composite PK.
- FK có đúng cascade behavior.
- Enum dùng `varchar`, không phải integer ngoài ý muốn.
- Unique/check/index có đúng tên.
- Không có column `xmin` được tạo thủ công.
- Có `CREATE SEQUENCE` trong `Up` và `DROP SEQUENCE` trong `Down`.
- Không có migration noise do seed dùng `Guid.NewGuid()` hoặc `UtcNow`.

`dotnet ef migrations add` là code generation hữu ích, nhưng migration sinh ra không phải chân lý. Intern nổi bật là người đọc và giải thích được migration trước khi chạy.

Không dùng `Database.EnsureCreated()`. Nó bỏ qua migration history và raw SQL tạo sequence.

## 13. Seed data deterministic

Dùng GUID và timestamp cố định:

```csharp
private static readonly Guid NetworkLabelId =
    Guid.Parse("10000000-0000-0000-0000-000000000001");
```

Không dùng `Guid.NewGuid()` hay `DateTimeOffset.UtcNow` trong `HasData`, vì mỗi lần scaffold migration EF sẽ nghĩ seed data đã thay đổi.

Nếu dùng startup seeder, thiết kế idempotent:

```text
Nếu slug đã tồn tại -> bỏ qua
Nếu chưa tồn tại    -> insert
```

Seeder không được phụ thuộc thứ tự chạy ngẫu nhiên và chạy lần hai không được tạo duplicate.

## 14. Integration test với PostgreSQL thật

EF InMemory không chứng minh được sequence, check constraint, unique index, `ILIKE`, transaction hoặc `xmin`. Các phần này là lý do chính của bài nên phải test với PostgreSQL thật.

### 14.1. Test pyramid phù hợp

```text
Ít API integration tests: HTTP contract + middleware
Nhiều service/database integration tests: invariant + query + race
Một số unit tests: slug, normalization, pagination math
```

### 14.2. Test constraint đúng tên

Đừng chỉ assert “có exception”. Assert PostgreSQL từ chối bởi đúng constraint:

```csharp
var exception = await Assert.ThrowsAsync<PostgresException>(() =>
    db.Database.ExecuteSqlRawAsync(sql));

Assert.Equal("ck_tickets_title_not_blank", exception.ConstraintName);
```

Nếu SQL sai tên cột cũng ném exception. Chỉ assert exception sẽ tạo test xanh giả.

### 14.3. Test concurrency bằng hai DbContext

```csharp
await using var first = factory.CreateDbContext();
await using var second = factory.CreateDbContext();

var mine = await first.Tickets.SingleAsync(x => x.Id == ticketId);
var theirs = await second.Tickets.SingleAsync(x => x.Id == ticketId);

theirs.Title = "Người kia lưu trước";
await second.SaveChangesAsync();

mine.Title = "Tôi lưu sau";
await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
    () => first.SaveChangesAsync());
```

API test bổ sung phải chứng minh exception được map thành `409 TICKET_CONCURRENCY_CONFLICT`.

### 14.4. Test sinh code song song

Tạo 20–50 task, mỗi task dùng scope/DbContext riêng, gọi create ticket đồng thời. Assert:

- Tất cả request thành công.
- Tất cả code distinct.
- Không giả định code liên tục nếu test có rollback/retry.

Không dùng chung một `DbContext` giữa các task; `DbContext` không thread-safe và đó sẽ là test sai đối tượng.

### 14.5. Test paging không lặp/mất dòng

Seed nhiều ticket có cùng `CreatedAt`, đọc page 1 và page 2, assert:

- Hai page không giao nhau.
- Hợp hai page đúng bằng tập mong đợi.
- Thứ tự tuân theo `CreatedAt DESC, Id DESC`.

### 14.6. Test replace labels rollback

Thiết lập ticket đang có label A. Gửi `[B, id-khong-ton-tai]`. Assert nhận `400` và ticket vẫn giữ A. Đây là test chứng minh “validate trước, save một lần” hoạt động.

## 15. Logging và khả năng quan sát

Mini project vẫn nên có structured logging:

```csharp
logger.LogInformation(
    "Created ticket {TicketId} with code {TicketCode}",
    ticket.Id,
    ticket.Code);
```

Không nối chuỗi log. Structured properties cho phép query theo `TicketId` hoặc `TicketCode`.

Không log toàn request body vì description/comment có thể chứa dữ liệu nhạy cảm. Nên log:

- HTTP method, path, status, elapsed time.
- Trace ID.
- Ticket ID/code khi use case thành công.
- Exception đầy đủ ở server.

Không log password database hoặc connection string.

## 16. Những phần hay từ project `document_first` nên áp dụng

| Pattern nguồn | Áp dụng vào Mini 01 | Giá trị học được |
|---|---|---|
| `ApplyConfigurationsFromAssembly` | Tự discover bốn EF configurations | Thêm entity không phải sửa một switch lớn |
| Timestamp trong `SaveChanges` | Tập trung `CreatedAt`/`UpdatedAt` | Tránh service quên audit field |
| `DocumentKeyGenerator` atomic | `TicketCodeGenerator` bằng sequence | Hiểu race condition và database atomicity |
| `xmin` shadow property | `rowVersion` trong response/update | Chống lost update |
| Query projection | List ticket không load graph | Hiểu SQL do LINQ sinh ra |
| Global exception middleware | Một error contract | Controller mỏng, client dễ xử lý |
| `InvalidModelStateResponseFactory` | Data Annotation cùng error shape | API contract nhất quán |
| Schema test đúng constraint name | Test check/unique/FK thật | Chứng minh DB invariant, tránh test xanh giả |

Không copy những phần không thuộc câu hỏi của Mini 01: JWT, RBAC, Quartz, Markdown, versioning, pgvector, GitHub hoặc generic architecture framework.

## 17. Tự động hóa nên có

“Tự động generate code” trong project này nên dùng đúng chỗ:

1. PostgreSQL sequence tự cấp số ticket an toàn.
2. EF Core scaffold migration từ Fluent Configuration.
3. Swagger/OpenAPI tự sinh tài liệu API từ controller và DTO.
4. `ApplyConfigurationsFromAssembly` tự đăng ký entity configurations.
5. `UseSnakeCaseNamingConvention` tự map PascalCase sang snake_case.
6. Timestamp được DbContext tự gán.
7. Có thể thêm OpenAPI client generation sau khi API contract ổn định, nhưng không phải MVP.

Không nên dùng source generator, reflection mapping framework hoặc code generator CRUD chỉ để giảm vài chục dòng. Mục tiêu Mini 01 là hiểu request → SQL → constraint → response; che mất chuỗi này sẽ làm giảm giá trị học tập.

Một script/command CI tối thiểu nên chạy:

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
```

Nếu có database riêng cho test, thêm bước migrate database rỗng. Test migration từ zero quan trọng hơn việc chỉ chạy trên database dev đã tồn tại lâu ngày.

## 18. Lộ trình 4 vòng để phát triển vượt bậc

### Vòng 1 — Làm đúng happy path

- Tạo solution và database.
- Viết entity/configuration/migration.
- Làm label, create ticket và detail.
- Demo được trên Swagger.

Đầu ra: một API chạy được.

### Vòng 2 — Làm đúng khi input xấu

- Validation ở request, service và database.
- Error response thống nhất.
- Test not found, duplicate slug, blank text, invalid label.

Đầu ra: một API có contract đáng tin.

### Vòng 3 — Làm đúng khi concurrent và dữ liệu lớn hơn

- Sequence thay `Count + 1`.
- `xmin` chống lost update.
- Stable paging.
- Projection và `AsNoTracking`.
- Parallel/concurrency integration tests.

Đầu ra: một API giải quyết vấn đề mà CRUD tutorial thường bỏ qua.

### Vòng 4 — Giải thích và đo lường

- Dùng `ToQueryString()` đọc SQL.
- Chạy `EXPLAIN (ANALYZE, BUFFERS)` cho list query với seed lớn.
- Viết một Architecture Decision Record ngắn cho sequence và `xmin`.
- Vẽ request flow và transaction boundary.
- Ghi trade-off còn tồn tại, ví dụ race giữa close và add comment.

Đầu ra: bạn không chỉ “biết làm” mà còn “biết chứng minh và giải thích”. Đây là điểm khác biệt rõ nhất giữa intern mạnh và người chỉ làm theo tutorial.

## 19. Câu hỏi tự kiểm tra

Nếu trả lời được bằng lời của mình, bạn đã hiểu project:

1. Vì sao `Count + 1` sai dù có unique index?
2. Vì sao sequence được phép có gap?
3. `xmin` thay đổi lúc nào và EF dùng nó trong `UPDATE` ra sao?
4. Vì sao check row version trong C# thôi vẫn có race?
5. Vì sao endpoint list không nên `Include(Comments)`?
6. Vì sao sort chỉ theo `CreatedAt` chưa đủ cho paging?
7. Vì sao application validation không thay thế DB constraint?
8. Vì sao cần assert đúng `ConstraintName` trong schema test?
9. Vì sao replace labels chỉ `SaveChangesAsync` một lần?
10. Vì sao không dùng chung `DbContext` trong parallel test?
11. Khi nào `409` đúng hơn `400`?
12. Index `(status, priority)` có giúp query chỉ filter `priority` không, và vì sao?

Câu 12 là bài tập đọc execution plan. Composite B-tree index thường hữu ích nhất từ left-most prefix; nếu workload chủ yếu lọc riêng priority, cần đo và cân nhắc index khác thay vì đoán.

## 20. Definition of Done nâng cao

### Database

- [ ] Database rỗng migrate lên được bằng toàn bộ migration.
- [ ] Có đúng bốn table nghiệp vụ và một sequence.
- [ ] PK, FK, cascade, unique, check constraint và index đúng thiết kế.
- [ ] `xmin` không bị tạo thành column thường.
- [ ] Seed chạy lặp lại không sinh duplicate hoặc migration noise.

### Application

- [ ] Code do server sinh bằng sequence.
- [ ] Ticket mới luôn `Open`.
- [ ] Closed ticket không nhận comment.
- [ ] Replace labels validate hết rồi save một lần.
- [ ] Update dùng row version và stale request trả `409`.
- [ ] Không trả EF entity trực tiếp.

### Query

- [ ] List dùng `AsNoTracking` và projection.
- [ ] Filter kết hợp bằng AND.
- [ ] `pageSize` giới hạn 1–100.
- [ ] Sort có tie-breaker theo `Id`.
- [ ] Không load comments chỉ để đếm.
- [ ] Đã xem SQL sinh ra cho query quan trọng.

### API

- [ ] HTTP status đúng `201/200/204/400/404/409/500`.
- [ ] Validation và exception cùng một error contract.
- [ ] Response có trace ID.
- [ ] Swagger demo được cả happy path và error path.
- [ ] Không rò stack trace hoặc secret.

### Tests

- [ ] Integration test dùng PostgreSQL thật.
- [ ] Test code song song không trùng.
- [ ] Test stale update bằng hai DbContext.
- [ ] Test constraint đúng tên.
- [ ] Test stable paging.
- [ ] Test replace label thất bại không làm mất state cũ.

### Kỹ năng trình bày

- [ ] README có sơ đồ ERD, cách chạy, demo và scope không làm.
- [ ] Có ít nhất hai ADR ngắn: ticket sequence và optimistic concurrency.
- [ ] Giải thích được SQL/query plan của list endpoint.
- [ ] Nêu được một trade-off chưa giải quyết và hướng nâng cấp.

## 21. Stretch goals sau khi hoàn thành MVP

Chỉ chọn sau khi toàn bộ Definition of Done đã xanh:

1. Keyset pagination theo `(created_at, id)` để tránh offset chậm ở page sâu.
2. State-transition matrix, ví dụ không cho `Closed → InProgress` nếu chưa reopen.
3. Audit log cho thay đổi status/assignee.
4. Idempotency key cho `POST /tickets`.
5. Conditional update bằng HTTP `ETag`/`If-Match` thay rowVersion trong JSON.
6. Test execution plan hoặc benchmark với 100.000 ticket.
7. Row lock/transaction strategy để đóng race giữa close ticket và add comment.

Không làm tất cả cùng lúc. Mỗi stretch goal phải bắt đầu bằng một câu hỏi đo được, ví dụ: “Offset paging chậm từ page nào với 100.000 rows?”

## 22. Kết luận

Giá trị lớn nhất của Mini 01 nằm ở sáu kỹ thuật:

1. Schema có constraint, không chỉ có entity.
2. Ticket code được cấp atomic bằng PostgreSQL sequence.
3. Query list dùng projection và stable paging.
4. `xmin` ngăn lost update.
5. Thao tác nhiều bước có transaction boundary rõ ràng.
6. Integration test chứng minh hành vi trên PostgreSQL thật.

Nếu làm chắc sáu phần này, project tuy nhỏ nhưng đủ thể hiện tư duy backend vượt xa một CRUD tutorial thông thường.
