namespace mini_1_helpdesk_ticket.Application.Labels;

public class Request
{
    public class CreateLabelRequest
    {
        public string Name { get; set; } = null!;
        public string Color { get; set; } = null!;
    }
}