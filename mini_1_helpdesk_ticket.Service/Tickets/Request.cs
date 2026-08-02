using System.ComponentModel.DataAnnotations;
using mini_1_helpdesk_ticket.Repo.Enum;

namespace mini_1_helpdesk_ticket.Service.Tickets;

public class Request
{
    public class CreateTicketRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required, MaxLength(5000)]
        public string Description { get; set; } = null!;

        public TicketPriority Priority { get; set; }

        [MaxLength(150)]
        public string? AssigneeName { get; set; }
    }

    public class UpdateTicketRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required, MaxLength(5000)]
        public string Description { get; set; } = null!;

        [Required]
        public TicketPriority Priority { get; set; }

        [Required]
        public TicketStatus Status { get; set; }

        [MaxLength(150)]
        public string? AssigneeName { get; set; }

        [Required]
        public uint RowVersion { get; set; }
    }

    public class AddCommentRequest
    {
        [Required, MaxLength(150)]
        public string AuthorName { get; set; } = null!;

        [Required, MaxLength(5000)]
        public string Content { get; set; } = null!;
    }

    public class ReplaceTicketLabelsRequest
    {
        [Required]
        //list of new LabelIds
        //it quite diff with ICollection: 
        //ICollection<T> trong Entity dùng để EF Core quản lý quan hệ 1-N. Nó cho phép thêm/xóa phần tử bằng Add(), Remove().
        // IReadOnlyCollection<T> trong Request DTO chỉ biểu diễn dữ liệu client gửi vào. Code nhận request chỉ cần đọc danh sách, không cần chỉnh sửa trực tiếp
        public IReadOnlyCollection<Guid> LabelIds { get; set; } = Array.Empty<Guid>();
    }

    public class TicketFilter
    {
        public TicketStatus? Status { get; set; }

        public TicketPriority? Priority { get; set; }

        public string? Assignee { get; set; }

        public string? Q { get; set; }

        public Guid? LabelId { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}