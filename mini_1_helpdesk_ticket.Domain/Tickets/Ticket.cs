using mini_1_helpdesk_ticket.Domain.Common;

namespace mini_1_helpdesk_ticket.Domain.Tickets;

public class Ticket: BaseEntity<Guid>, IAuditableEntity
{
    public string Code { get; set; } = null!;
    public string Title { get; private set; } = null!;
    public string Description { get; set; } = null!;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketStatus Status { get; private set; } = TicketStatus.Open;
    public string? AssigneeName { get; set; } 
    public uint Version { get; private set; }
    
    public ICollection<TicketLabel> TicketLabels { get; set; } = new List<TicketLabel>();

    private readonly List<TicketComment> _ticketComments = [];

    public IReadOnlyCollection<TicketComment> TicketComments
        => _ticketComments;
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public static Ticket Create(Guid id, string code,
        string title, string description, TicketPriority priority, string? assigneeName)
    {
        ValidateRequired(code, nameof(code));
        ValidateRequired(title, nameof(title));
        ValidateRequired(description, nameof(description));

        return new Ticket
        {
            Id = id,
            Code = code.Trim(),
            Title = title.Trim(),
            Description = description.Trim(),
            Priority = priority,
            Status = TicketStatus.Open,
            AssigneeName = string.IsNullOrWhiteSpace(assigneeName)
                ? null
                : assigneeName.Trim(),
        };
    }

    public TicketComment AddComment(Guid commentId, string authorName, string content)
    {
        if (Status == TicketStatus.Closed)
        {
            throw new DomainException(
                "TICKET_CLOSED",
                "Ticket đã đóng và không nhận comment mới");
        }
        
        ValidateRequired(authorName, nameof(authorName));
        ValidateRequired(content, nameof(content));

        var comment = new TicketComment
        {
            Id = commentId,
            TicketId = Id,
            Ticket = this,
            AuthorName = authorName.Trim(),
            Content = content.Trim(),
        };
        
        _ticketComments.Add(comment);
        
        return comment;
    }

    public void UpdateDetails(string title,
        string description, TicketPriority priority,
        TicketStatus status, string? assigneeName)
    {
        ValidateRequired(title, nameof(title));
        ValidateRequired(description, nameof(description));
        
        Title = title.Trim();
        Description = description.Trim();
        Priority = priority;
        Status = status;
        AssigneeName = string.IsNullOrWhiteSpace(assigneeName)
            ? null 
            : assigneeName.Trim();
    }
    
    private static void ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                "TICKET_FIELD_IS_REQUIRED",
                $"{fieldName} không được để trống. ");
        }
    }
}


