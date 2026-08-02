using mini_1_helpdesk_ticket.Repo.Abstraction;
using mini_1_helpdesk_ticket.Repo.Enum;
using Superpower.Parsers;

namespace mini_1_helpdesk_ticket.Repo.Entity;

public class Ticket: BaseEntity<Guid>, IAuditableEntity
{
    public string Code { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; }
    public string AssigneeName { get; set; } = null!;
    
    public ICollection<TicketLabel> TicketLabels { get; set; } = new List<TicketLabel>();
    public ICollection<TicketComment> TicketComments { get; set; } = new List<TicketComment>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}