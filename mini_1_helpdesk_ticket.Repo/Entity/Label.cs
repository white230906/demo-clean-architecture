using mini_1_helpdesk_ticket.Repo.Abstraction;

namespace mini_1_helpdesk_ticket.Repo.Entity;

public class Label: BaseEntity<Guid>, IAuditableEntity
{
    
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Color { get; set; }
    
    public ICollection<TicketLabel> TicketLabels { get; set; } = new List<TicketLabel>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}