using mini_1_helpdesk_ticket.Application.Common.Models;
using mini_1_helpdesk_ticket.Domain.Tickets;

namespace mini_1_helpdesk_ticket.Application.Tickets;

public class Request
{
    public class CreateTicketRequest
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public TicketPriority Priority { get; set; }
        public string? AssigneeName { get; set; }
    }

    public class UpdateTicketRequest
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public TicketPriority Priority { get; set; }
        public TicketStatus Status { get; set; }
        public string? AssigneeName { get; set; }
        public uint RowVersion { get; set; }
    }

    public class AddCommentRequest
    {
        public string AuthorName { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    public class ReplaceTicketLabelsRequest
    {
        public IReadOnlyCollection<Guid> LabelIds { get; set; } = [];
    }

    public class TicketFilter : PaginationRequest
    {
        public TicketStatus? Status { get; set; }
        public TicketPriority? Priority { get; set; }
        public string? Assignee { get; set; }
        public string? Q { get; set; }
        public Guid? LabelId { get; set; }
    }
}
