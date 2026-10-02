namespace mini_1_helpdesk_ticket.Domain.Common;

public abstract class BaseEntity<TKey>
{
    public TKey Id { get; set; } = default!;
    
    public bool IsDeleted  { get; set; }
}