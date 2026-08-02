using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mini_1_helpdesk_ticket.Repo.Entity;
using mini_1_helpdesk_ticket.Repo.Enum;

namespace mini_1_helpdesk_ticket.Repo.Configurations;

public class TicketConfiguration: IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets", table =>
        {
            table.HasCheckConstraint(
                "ck_tickets_title_not_blank",
                "length(brim(title)) > 0");
            
            table.HasCheckConstraint(
                "ck_tickets_description_not_blank",
                "length(brim(description)) > 0");

            table.HasCheckConstraint(
                "ck_tickets_priority_valid",
                "priority IN ('Low','Medium','High', 'Urgent')");

            table.HasCheckConstraint(
                "ck_tickets_status_valid",
                "status IN ('Open','Closed','Resolved','InProgress')");
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
        
        //config: chống xung đột giữ liệu - Optimistic Concurrency
        builder.Property<uint>("xmin")
            .IsRowVersion()
            .HasColumnName("xmin")
            .HasColumnType("xid");
        //Đây là một cấu hình đặc thù dành cho PostgreSQL. Xmin làm cột ẩn mặc định trong PostgreSql
        //nó tự động thay đổi giá trị mỗi khi dòng đó được update
        //Mục đích dùng để triển khai: Optimistic Concurrency Control(Kiểm soát bất đồng bộ lạc quan)
        //Case: User A và User B cùng config chung 1 cái ticket, UserA ấn lưu trước.
            //Khi đến lượt UserB ấn lưu thì, thì EF Core kiểm tra thấy xmin đã thay đổi nên sẽ quăng ra lỗi
            //DbUpdateConcurrenctException chứ không ghi đè lên dữ liệu của User A.
            
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("ix_tickets_code");
        
        builder.HasIndex(x => new { x.CreatedAt, x.Id})
            .IsDescending()
            .HasDatabaseName("ix_tickets_created_at_id");
        
        builder.HasIndex(x => new {x.Status, x.Priority})
            .HasDatabaseName("ix_tickets_status_priority");
        //dùng để đặt tên riêng cho index đó trực tiếp trong db
        
        builder.HasIndex(x => x.AssigneeName)
            .HasDatabaseName("ix_tickets_assignee_name");
        
    }
}