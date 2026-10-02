using mini_1_helpdesk_ticket.Domain.Common;
using mini_1_helpdesk_ticket.Domain.Tickets;
using Xunit;

namespace mini_1_helpdesk_ticket.Test.Tickets;

public sealed class TicketTests
{
    [Fact]
    public void Create_WithValidData_TrimsValuesAndStartsOpen()
    {
        var id = Guid.NewGuid();

        var ticket = Ticket.Create(
            id,
            "  TCK-0001  ",
            "  VPN unavailable  ",
            "  Cannot connect to VPN  ",
            TicketPriority.High,
            "  Alice  ");

        Assert.Equal(id, ticket.Id);
        Assert.Equal("TCK-0001", ticket.Code);
        Assert.Equal("VPN unavailable", ticket.Title);
        Assert.Equal("Cannot connect to VPN", ticket.Description);
        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Equal("Alice", ticket.AssigneeName);
        Assert.Empty(ticket.TicketComments);
        Assert.Empty(ticket.TicketLabels);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankAssignee_NormalizesAssigneeToNull(string? assigneeName)
    {
        var ticket = CreateTicket(assigneeName);

        Assert.Null(ticket.AssigneeName);
    }

    [Theory]
    [InlineData("", "Title", "Description")]
    [InlineData("Code", "   ", "Description")]
    [InlineData("Code", "Title", "")]
    public void Create_WhenRequiredFieldIsBlank_ThrowsDomainException(
        string code,
        string title,
        string description)
    {
        var exception = Assert.Throws<DomainException>(() => Ticket.Create(
            Guid.NewGuid(),
            code,
            title,
            description,
            TicketPriority.Medium,
            null));

        Assert.Equal("TICKET_FIELD_IS_REQUIRED", exception.Code);
    }

    [Fact]
    public void UpdateDetails_WithValidData_UpdatesAndTrimsValues()
    {
        var ticket = CreateTicket();

        ticket.UpdateDetails(
            "  Updated title  ",
            "  Updated description  ",
            TicketPriority.Urgent,
            TicketStatus.InProgress,
            "  Bob  ");

        Assert.Equal("Updated title", ticket.Title);
        Assert.Equal("Updated description", ticket.Description);
        Assert.Equal(TicketPriority.Urgent, ticket.Priority);
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal("Bob", ticket.AssigneeName);
    }

    [Fact]
    public void UpdateDetails_WhenDescriptionIsBlank_DoesNotPartiallyUpdateTicket()
    {
        var ticket = CreateTicket();

        var exception = Assert.Throws<DomainException>(() => ticket.UpdateDetails(
            "New title",
            "   ",
            TicketPriority.High,
            TicketStatus.Resolved,
            "Bob"));

        Assert.Equal("TICKET_FIELD_IS_REQUIRED", exception.Code);
        Assert.Equal("VPN unavailable", ticket.Title);
        Assert.Equal("Cannot connect to VPN", ticket.Description);
        Assert.Equal(TicketPriority.Medium, ticket.Priority);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Null(ticket.AssigneeName);
    }

    [Fact]
    public void AddComment_WhenTicketIsOpen_AddsTrimmedComment()
    {
        var ticket = CreateTicket();
        var commentId = Guid.NewGuid();

        var comment = ticket.AddComment(
            commentId,
            "  Alice  ",
            "  Please check the VPN gateway.  ");

        Assert.Equal(commentId, comment.Id);
        Assert.Equal(ticket.Id, comment.TicketId);
        Assert.Same(ticket, comment.Ticket);
        Assert.Equal("Alice", comment.AuthorName);
        Assert.Equal("Please check the VPN gateway.", comment.Content);
        Assert.Same(comment, Assert.Single(ticket.TicketComments));
    }

    [Fact]
    public void AddComment_WhenTicketIsClosed_ThrowsAndDoesNotAddComment()
    {
        var ticket = CreateTicket();
        ticket.UpdateDetails(
            ticket.Title,
            ticket.Description,
            ticket.Priority,
            TicketStatus.Closed,
            ticket.AssigneeName);

        var exception = Assert.Throws<DomainException>(() => ticket.AddComment(
            Guid.NewGuid(),
            "Alice",
            "A new comment"));

        Assert.Equal("TICKET_CLOSED", exception.Code);
        Assert.Empty(ticket.TicketComments);
    }

    [Theory]
    [InlineData("", "Content")]
    [InlineData("Alice", "   ")]
    public void AddComment_WhenRequiredFieldIsBlank_ThrowsWithoutAddingComment(
        string authorName,
        string content)
    {
        var ticket = CreateTicket();

        var exception = Assert.Throws<DomainException>(() => ticket.AddComment(
            Guid.NewGuid(),
            authorName,
            content));

        Assert.Equal("TICKET_FIELD_IS_REQUIRED", exception.Code);
        Assert.Empty(ticket.TicketComments);
    }

    private static Ticket CreateTicket(string? assigneeName = null) => Ticket.Create(
        Guid.NewGuid(),
        "TCK-0001",
        "VPN unavailable",
        "Cannot connect to VPN",
        TicketPriority.Medium,
        assigneeName);
}
