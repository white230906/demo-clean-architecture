namespace mini_1_helpdesk_ticket.Domain.Common;

public interface IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; } 
    public DateTimeOffset? UpdatedAt { get; set; } 
}