namespace mini_1_helpdesk_ticket.Application.Labels;

public class Response
{
    public class LabelResponse
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public required string Color { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
