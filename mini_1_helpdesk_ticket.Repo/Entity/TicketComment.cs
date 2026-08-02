using mini_1_helpdesk_ticket.Repo.Abstraction;

namespace mini_1_helpdesk_ticket.Repo.Entity;

public class TicketComment: BaseEntity<Guid>, IAuditableEntity
{
    public Guid TicketId { get; set; }
    public string AuthorName { get; set; } = null!;
    public string Content { get; set; } = null!;

    public Ticket Ticket { get; set; } = null!;
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}