using Label = mini_1_helpdesk_ticket.Domain.Labels.Label;

namespace mini_1_helpdesk_ticket.Domain.Tickets;

public class TicketLabel
{
    public Guid TicketId { get; set; }
    public Guid LabelId { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public Label Label { get; set; } = null!; 
}