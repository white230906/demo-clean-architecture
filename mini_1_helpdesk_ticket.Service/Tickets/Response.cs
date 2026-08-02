namespace mini_1_helpdesk_ticket.Service.Tickets;

using mini_1_helpdesk_ticket.Repo.Enum;
using LabelResponse =  Labels.Response.LabelResponse;

public class Response
{
    public class CommentResponse
    {
        public Guid Id { get; set; }

        public Guid TicketId { get; set; }

        public string AuthorName { get; set; } = null!;

        public string Content { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class TicketListItemResponse
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = null!;

        public string Title { get; set; } = null!;

        public TicketPriority Priority { get; set; }

        public TicketStatus Status { get; set; }

        public string? AssigneeName { get; set; }

        public int CommentCount { get; set; }

        public IReadOnlyList<LabelResponse> Labels { get; set; } = Array.Empty<LabelResponse>();

        public uint RowVersion { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public class TicketDetailResponse
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;

        public TicketPriority Priority { get; set; }

        public TicketStatus Status { get; set; }

        public string? AssigneeName { get; set; }

        public uint RowVersion { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public IReadOnlyList<CommentResponse> Comments { get; set; } = Array.Empty<CommentResponse>();

        public IReadOnlyList<LabelResponse> Labels { get; set; } = Array.Empty<LabelResponse>();
    }

    public class TicketLabelsResponse
    {
        public Guid TicketId { get; set; }

        public IReadOnlyList<LabelResponse> Labels { get; set; } = Array.Empty<LabelResponse>();
    }
}