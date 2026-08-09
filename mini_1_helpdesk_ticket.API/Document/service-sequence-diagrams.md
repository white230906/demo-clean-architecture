# Sequence diagram cho Labels.Service và Tickets.Service

Tài liệu này mô tả 10 hàm nghiệp vụ public được khai báo trong hai class Service thuộc các folder Labels và Tickets. Mỗi mục ghi rõ hàm tương ứng ngay phía trên code Mermaid.

## Database ER diagram

Sơ đồ dưới đây mô tả bốn bảng được sử dụng bởi Labels.Service và Tickets.Service.

```mermaid
erDiagram
    TICKETS {
        uuid id PK
        varchar code UK
        varchar title
        text description
        varchar priority
        varchar status
        varchar assignee_name
        boolean is_deleted
        datetime created_at
        datetime updated_at
        xid xmin
    }

    TICKET_COMMENTS {
        uuid id PK
        uuid ticket_id FK
        varchar author_name
        text content
        boolean is_deleted
        datetime created_at
        datetime updated_at
    }

    LABELS {
        uuid id PK
        varchar name
        varchar slug UK
        varchar color
        boolean is_deleted
        datetime created_at
        datetime updated_at
    }

    TICKET_LABELS {
        uuid ticket_id PK, FK
        uuid label_id PK, FK
    }

    TICKETS ||--o{ TICKET_COMMENTS : has
    TICKETS ||--o{ TICKET_LABELS : tagged_with
    LABELS ||--o{ TICKET_LABELS : assigned_to
```

## Labels.Service

### Hàm: GetLabels(CancellationToken ct)

```mermaid
sequenceDiagram
    actor Caller
    participant Service as Labels.Service
    participant DbContext as HelpdeskDbContext
    participant DB as PostgreSQL

    Caller->>Service: GetLabels(ct)
    Service->>DbContext: Tạo query Labels.AsNoTracking()
    Service->>DbContext: Lọc IsDeleted = false
    Service->>DbContext: Sắp xếp CreatedAt và map LabelResponse
    DbContext->>DB: SELECT labels
    DB-->>DbContext: Các label phù hợp
    DbContext-->>Service: ToListAsync(ct)
    Service-->>Caller: IReadOnlyList LabelResponse
```

### Hàm: CreateLabel(CreateLabelRequest request, CancellationToken ct)

```mermaid
sequenceDiagram
    actor Caller
    participant Service as Labels.Service
    participant TextRules
    participant DbContext as HelpdeskDbContext
    participant DB as PostgreSQL

    Caller->>Service: CreateLabel(request, ct)
    Service->>TextRules: Require(request.Name)
    TextRules-->>Service: name hợp lệ
    Service->>TextRules: ToSlug(name)
    TextRules-->>Service: slug
    Service->>Service: Tạo Label với Guid mới và Color.Trim()
    Service->>DbContext: Labels.Add(label)

    loop Cho tới khi lưu thành công
        Service->>DbContext: SaveChangesAsync(ct)
        DbContext->>DB: INSERT label
        alt Vi phạm unique constraint ux_labels_slug
            DB-->>DbContext: PostgreSQL 23505 UniqueViolation
            DbContext-->>Service: DbUpdateException
            Service->>Service: Nối hậu tố -i vào slug hiện tại
            Service->>Service: Gán lại label.Slug
        else Lưu thành công
            DB-->>DbContext: Label đã được lưu
            DbContext-->>Service: Thành công
        end
    end

    Service->>Service: Map Label thành LabelResponse
    Service-->>Caller: LabelResponse
```

### Hàm: DeleteLabels(List&lt;Guid&gt; labelIds, CancellationToken ct)

```mermaid
sequenceDiagram
    actor Caller
    participant Service as Labels.Service
    participant DbContext as HelpdeskDbContext
    participant DB as PostgreSQL

    Caller->>Service: DeleteLabels(labelIds, ct)
    Service->>DbContext: Query label có Id nằm trong labelIds
    Service->>DbContext: Labels.RemoveRange(deleteLabels)
    DbContext->>DB: SELECT các label cần xóa
    DB-->>DbContext: Các label tồn tại
    Service->>DbContext: SaveChangesAsync(ct)
    DbContext->>DB: DELETE labels
    DB-->>DbContext: Hoàn tất xóa
    Service->>DbContext: deleteLabels.Count()
    DbContext->>DB: COUNT label còn khớp query
    DB-->>DbContext: Số label còn lại
    Service->>Service: result = labelIds.Count - count
    Service-->>Caller: Chuỗi Xóa {result} phần tử
```

## Tickets.Service

### Hàm: CreateTicket(CreateTicketRequest request, CancellationToken ct)

```mermaid
sequenceDiagram
    actor Caller
    participant Service as Tickets.Service
    participant CodeGenerator as TicketCodeGenerator
    participant TextRules
    participant DbContext as HelpdeskDbContext
    participant DB as PostgreSQL

    Caller->>Service: CreateTicket(request, ct)
    Service->>CodeGenerator: NextAsync(ct)
    CodeGenerator->>DbContext: SqlQueryRaw nextval(ticket_code_seq)
    DbContext->>DB: SELECT nextval
    DB-->>CodeGenerator: sequence number
    CodeGenerator-->>Service: TCK-{number}
    Service->>TextRules: Require Title và Description
    TextRules-->>Service: Giá trị hợp lệ
    Service->>TextRules: NullIfWhiteSpace(AssigneeName)
    TextRules-->>Service: AssigneeName đã chuẩn hóa
    Service->>Service: Tạo Ticket, Status = Open
    Service->>DbContext: Tickets.AddAsync(newTicket, ct)
    Service->>DbContext: SaveChangesAsync(ct)
    DbContext->>DB: INSERT ticket
    DB-->>DbContext: Ticket đã lưu
    Service->>Service: GetTicket(newTicket.Id, ct)
    Service->>DbContext: Query ticket mới, comments, labels và xmin
    DbContext->>DB: SELECT ticket detail
    DB-->>DbContext: Ticket vừa tạo
    DbContext-->>Service: TicketDetailResponse
    Service-->>Caller: TicketDetailResponse
```

### Hàm: GetTicket(Guid id, CancellationToken ct)

```mermaid
sequenceDiagram
    actor Caller
    participant Service as Tickets.Service
    participant DbContext as HelpdeskDbContext
    participant DB as PostgreSQL

    Caller->>Service: GetTicket(id, ct)
    Service->>DbContext: Query ticket theo id và IsDeleted = false
    Service->>DbContext: Project ticket, xmin, comments và labels
    DbContext->>DB: SELECT ticket detail
    DB-->>DbContext: Không có hoặc một ticket
    DbContext-->>Service: SingleOrDefaultAsync(ct)
    alt Tìm thấy ticket
        Service-->>Caller: TicketDetailResponse
    else Không tìm thấy
        Service-->>Caller: NotFoundException TICKET_NOT_FOUND
    end
```

### Hàm: GetTickets(TicketFilter filter, CancellationToken ct)

```mermaid
sequenceDiagram
    actor Caller
    participant Service as Tickets.Service
    participant DbContext as HelpdeskDbContext
    participant DB as PostgreSQL
    participant Factory as ApiResponseFactory

    Caller->>Service: GetTickets(filter, ct)
    alt PageIndex nhỏ hơn 1 hoặc PageSize ngoài 1..100
        Service-->>Caller: BadRequestException VALIDATION_FAILED
    else Phân trang hợp lệ
        Service->>DbContext: Bắt đầu Tickets.AsNoTracking()
        opt Có Status
            Service->>DbContext: Lọc theo Status
        end
        opt Có Priority
            Service->>DbContext: Lọc theo Priority
        end
        opt Có Assignee
            Service->>DbContext: ILike AssigneeName
        end
        opt Có từ khóa Q
            Service->>DbContext: ILike Code, Description hoặc Title
        end
        opt Có LabelId khác Guid.Empty
            Service->>DbContext: Lọc ticket chứa LabelId
        end
        Service->>DbContext: CountAsync(ct)
        DbContext->>DB: SELECT COUNT với các bộ lọc
        DB-->>Service: totalCount
        Service->>DbContext: OrderByDescending CreatedAt
        Service->>DbContext: Skip, Take và map item, labels, xmin
        DbContext->>DB: SELECT trang dữ liệu
        DB-->>Service: Danh sách TicketListItemResponse
        Service->>Factory: BasePagination(items, page, size, totalCount)
        Factory-->>Service: BasePaginationResponse
        Service-->>Caller: BasePaginationResponse
    end
```

### Hàm: UpdateTicket(Guid id, UpdateTicketRequest request, CancellationToken ct)

```mermaid
sequenceDiagram
    actor Caller
    participant Service as Tickets.Service
    participant TextRules
    participant DbContext as HelpdeskDbContext
    participant DB as PostgreSQL

    Caller->>Service: UpdateTicket(id, request, ct)
    alt RowVersion = 0
        Service-->>Caller: BadRequestException VALIDATION_FAILED
    else Có RowVersion
        Service->>DbContext: Tìm ticket theo id
        DbContext->>DB: SELECT ticket
        DB-->>Service: Ticket hoặc null
        alt Không tìm thấy ticket
            Service-->>Caller: BadRequestException TICKET_NOT_FOUND
        else Tìm thấy ticket
            Service->>DbContext: Đặt OriginalValue(xmin) = RowVersion
            Service->>TextRules: Validate Title và Description
            TextRules-->>Service: Giá trị hợp lệ
            Service->>TextRules: Chuẩn hóa AssigneeName
            TextRules-->>Service: AssigneeName
            Service->>Service: Cập nhật các thuộc tính ticket
            Service->>DbContext: SaveChangesAsync(ct)
            DbContext->>DB: UPDATE với optimistic concurrency xmin
            alt xmin đã thay đổi
                DB-->>DbContext: Không cập nhật được bản ghi
                DbContext-->>Service: DbUpdateConcurrencyException
                Service-->>Caller: ConflictException TICKET_CONCURRENCY_CONFLICT
            else Cập nhật thành công
                DB-->>DbContext: Ticket đã cập nhật
                Service->>Service: GetTicket(id, ct)
                Service->>DbContext: Query ticket, comments, labels và xmin mới
                DbContext->>DB: SELECT ticket detail
                DB-->>DbContext: Ticket mới nhất
                DbContext-->>Service: TicketDetailResponse
                Service-->>Caller: TicketDetailResponse mới nhất
            end
        end
    end
```

### Hàm: AddCommentTicket(Guid ticketId, AddCommentRequest request, CancellationToken ct)

```mermaid
sequenceDiagram
    actor Caller
    participant Service as Tickets.Service
    participant TextRules
    participant DbContext as HelpdeskDbContext
    participant DB as PostgreSQL

    Caller->>Service: AddCommentTicket(ticketId, request, ct)
    Service->>DbContext: Tìm ticket theo ticketId
    DbContext->>DB: SELECT ticket
    DB-->>Service: Ticket hoặc null
    alt Không tìm thấy ticket
        Service-->>Caller: NotFoundException TICKET_NOT_FOUND
    else Ticket đã Closed
        Service-->>Caller: ConflictException TICKET_CLOSED
    else Ticket nhận comment
        Service->>TextRules: Require AuthorName và Content
        TextRules-->>Service: Giá trị hợp lệ
        Service->>Service: Tạo TicketComment với Guid mới
        Service->>DbContext: TicketComments.Add(newComment)
        Service->>DbContext: SaveChangesAsync(ct)
        DbContext->>DB: INSERT ticket_comment
        DB-->>DbContext: Comment đã lưu
        Service->>Service: Map CommentResponse
        Service-->>Caller: CommentResponse
    end
```

### Hàm: ReplaceLabelsTicket(Guid ticketId, ReplaceTicketLabelsRequest request, CancellationToken ct)

```mermaid
sequenceDiagram
    actor Caller
    participant Service as Tickets.Service
    participant DbContext as HelpdeskDbContext
    participant DB as PostgreSQL

    Caller->>Service: ReplaceLabelsTicket(ticketId, request, ct)
    Service->>Service: labelIds = request.LabelIds hoặc mảng rỗng
    alt Danh sách có ID trùng
        Service-->>Caller: BadRequestException LABEL_IDS_INVALID
    else ID không trùng
        Service->>DbContext: Tìm ticket và Include TicketLabels
        DbContext->>DB: SELECT ticket và liên kết hiện tại
        DB-->>Service: Ticket hoặc null
        alt Không tìm thấy ticket
            Service-->>Caller: NotFoundException TICKET_NOT_FOUND
        else Tìm thấy ticket
            Service->>DbContext: Lấy và sắp xếp labels theo labelIds
            DbContext->>DB: SELECT labels
            DB-->>Service: Labels tồn tại
            alt Số label tìm được khác số ID yêu cầu
                Service-->>Caller: BadRequestException LABEL_IDS_INVALID
            else Tất cả label tồn tại
                Service->>Service: Tính requestedIds và currentIds
                Service->>Service: Tính linksToRemove và linksToAdd
                Service->>DbContext: TicketLabels.RemoveRange(linksToRemove)
                Service->>DbContext: TicketLabels.AddRange(linksToAdd)
                Service->>DbContext: SaveChangesAsync(ct)
                DbContext->>DB: DELETE liên kết cũ và INSERT liên kết mới
                DB-->>DbContext: Đã đồng bộ liên kết
                Service->>Service: Map labels thành LabelResponse
                Service-->>Caller: TicketLabelsResponse
            end
        end
    end
```

### Hàm: DeleteTicket(Guid id, CancellationToken ct)

```mermaid
sequenceDiagram
    actor Caller
    participant Service as Tickets.Service
    participant DbContext as HelpdeskDbContext
    participant DB as PostgreSQL

    Caller->>Service: DeleteTicket(id, ct)
    Service->>DbContext: Tìm ticket theo id
    DbContext->>DB: SELECT ticket
    DB-->>Service: Ticket hoặc null
    alt Không tìm thấy ticket
        Service-->>Caller: NotFoundException TICKET_NOT_FOUND
    else Tìm thấy ticket
        Service->>DbContext: Tickets.Remove(ticket)
        Service->>DbContext: SaveChangesAsync(ct)
        DbContext->>DB: DELETE ticket
        DB-->>DbContext: Xóa thành công
        Service-->>Caller: Hoàn tất
    end
```
