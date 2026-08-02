using mini_1_helpdesk_ticket.Repo.Abstraction;

namespace mini_1_helpdesk_ticket.Repo.Entity;

public class TicketLabel: BaseEntity<Guid>
{
    public Guid TicketId { get; set; }
    public Guid LabelId { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public Label Label { get; set; } = null!; 
}