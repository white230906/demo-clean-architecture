namespace mini_1_helpdesk_ticket.Service.Labels;

public class Response
{
    public class LabelResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Color { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
    }
}