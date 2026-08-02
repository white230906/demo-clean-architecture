namespace mini_1_helpdesk_ticket.Repo.Abstraction;

public interface IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; } 
    public DateTimeOffset? UpdatedAt { get; set; } 
}