using mini_1_helpdesk_ticket.Domain.Common;

namespace mini_1_helpdesk_ticket.Domain.Tickets;

public class TicketComment: BaseEntity<Guid>, IAuditableEntity
{
    //private set  -> ko ai có thể set ngoài class chứa nó
    //internal set -> ọi class trong cùng project được gán
    //public set   -> layer nào cũng gán được
    public Guid TicketId { get; internal set; }
    public string AuthorName { get; internal set; } = null!;
    public string Content { get; internal set; } = null!;

    public Ticket Ticket { get; internal set; } = null!;
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
