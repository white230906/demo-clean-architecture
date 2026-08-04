using Microsoft.EntityFrameworkCore;
using mini_1_helpdesk_ticket.Repo;
using mini_1_helpdesk_ticket.Repo.Entity;
using mini_1_helpdesk_ticket.Repo.Enum;
using mini_1_helpdesk_ticket.Service.Exceptions;
using TicketRequest = mini_1_helpdesk_ticket.Service.Tickets.Request;
using TicketService = mini_1_helpdesk_ticket.Service.Tickets.Service;
using Xunit;

namespace mini_1_helpdesk_ticket.Test.TicketTesting;

public sealed class TicketServiceTests
{
    [Fact]
    public async Task GetTickets_WhenPaginationIsInvalid_ThrowsBadRequest()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var filter = new TicketRequest.TicketFilter { PageIndex = 0, PageSize = 20 };

        var exception = await Assert.ThrowsAsync<CommonException.BadRequestException>(
            () => service.GetTickets(filter, CancellationToken.None));

        Assert.Equal("VALIDATION_FAILED", exception.MessageCode);
    }

    [Fact]
    public async Task GetTicket_WhenTicketDoesNotExist_ThrowsNotFound()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<CommonException.NotFoundException>(
            () => service.GetTicket(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("TICKET_NOT_FOUND", exception.MessageCode);
    }

    [Fact]
    public async Task AddCommentTicket_WhenTicketIsOpen_AddsTrimmedComment()
    {
        await using var db = CreateDbContext();
        var ticket = NewTicket(TicketStatus.Open);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var request = new TicketRequest.AddCommentRequest
        {
            AuthorName = "  An  ",
            Content = "  Kiểm tra kết nối  "
        };

        var result = await service.AddCommentTicket(ticket.Id, request, CancellationToken.None);

        Assert.Equal(ticket.Id, result.TicketId);
        Assert.Equal("An", result.AuthorName);
        Assert.Equal("Kiểm tra kết nối", result.Content);
        Assert.NotEqual(default, result.CreatedAt);
        Assert.Single(await db.TicketComments.ToListAsync());
    }

    [Fact]
    public async Task AddCommentTicket_WhenTicketIsClosed_ThrowsConflict()
    {
        await using var db = CreateDbContext();
        var ticket = NewTicket(TicketStatus.Closed);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var request = new TicketRequest.AddCommentRequest
        {
            AuthorName = "An",
            Content = "Comment mới"
        };

        var exception = await Assert.ThrowsAsync<CommonException.ConflictException>(
            () => service.AddCommentTicket(ticket.Id, request, CancellationToken.None));

        Assert.Equal("TICKET_CLOSED", exception.MessageCode);
        Assert.Empty(db.TicketComments);
    }

    [Fact]
    public async Task ReplaceLabelsTicket_WhenTicketHasNoLabels_AddsTwoLabels()
    {
        await using var db = CreateDbContext();
        var ticket = NewTicket();
        var first = NewLabel("Network", "network");
        var second = NewLabel("Hardware", "hardware");
        db.AddRange(ticket, first, second);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = CreateService(db);
        var request = new TicketRequest.ReplaceTicketLabelsRequest
        {
            LabelIds = new[] { first.Id, second.Id }
        };

        var result = await service.ReplaceLabelsTicket(ticket.Id, request, CancellationToken.None);

        Assert.Equal(ticket.Id, result.TicketId);
        Assert.Equal(2, result.Labels.Count);
        var savedIds = await db.TicketLabels
            .Where(x => x.TicketId == ticket.Id)
            .Select(x => x.LabelId)
            .ToListAsync();
        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, savedIds.ToHashSet());
    }

    [Fact]
    public async Task ReplaceLabelsTicket_ReplacesOldLabelWithNewLabel()
    {
        await using var db = CreateDbContext();
        var ticket = NewTicket();
        var oldLabel = NewLabel("Old", "old");
        var newLabel = NewLabel("New", "new");
        db.AddRange(ticket, oldLabel, newLabel);
        db.TicketLabels.Add(new TicketLabel
        {
            TicketId = ticket.Id,
            LabelId = oldLabel.Id
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = CreateService(db);
        var request = new TicketRequest.ReplaceTicketLabelsRequest
        {
            LabelIds = new[] { newLabel.Id }
        };

        await service.ReplaceLabelsTicket(ticket.Id, request, CancellationToken.None);

        var saved = await db.TicketLabels.SingleAsync(x => x.TicketId == ticket.Id);
        Assert.Equal(newLabel.Id, saved.LabelId);
    }

    [Fact]
    public async Task ReplaceLabelsTicket_WhenIdsAreDuplicated_ThrowsBadRequest()
    {
        await using var db = CreateDbContext();
        var ticket = NewTicket();
        var label = NewLabel("Network", "network");
        db.AddRange(ticket, label);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var request = new TicketRequest.ReplaceTicketLabelsRequest
        {
            LabelIds = new[] { label.Id, label.Id }
        };

        var exception = await Assert.ThrowsAsync<CommonException.BadRequestException>(
            () => service.ReplaceLabelsTicket(ticket.Id, request, CancellationToken.None));

        Assert.Equal("LABEL_IDS_INVALID", exception.MessageCode);
        Assert.Empty(db.TicketLabels);
    }

    [Fact]
    public async Task ReplaceLabelsTicket_WhenLabelDoesNotExist_ThrowsBadRequest()
    {
        await using var db = CreateDbContext();
        var ticket = NewTicket();
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var request = new TicketRequest.ReplaceTicketLabelsRequest
        {
            LabelIds = new[] { Guid.NewGuid() }
        };

        var exception = await Assert.ThrowsAsync<CommonException.BadRequestException>(
            () => service.ReplaceLabelsTicket(ticket.Id, request, CancellationToken.None));

        Assert.Equal("LABEL_IDS_INVALID", exception.MessageCode);
        Assert.Empty(db.TicketLabels);
    }

    [Fact]
    public async Task UpdateTicket_WhenRowVersionIsZero_ThrowsBadRequest()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var request = new TicketRequest.UpdateTicketRequest
        {
            Title = "Updated",
            Description = "Updated description",
            Priority = TicketPriority.High,
            Status = TicketStatus.InProgress,
            RowVersion = 0
        };

        var exception = await Assert.ThrowsAsync<CommonException.BadRequestException>(
            () => service.UpdateTicket(Guid.NewGuid(), request, CancellationToken.None));

        Assert.Equal("VALIDATION_FAILED", exception.MessageCode);
    }

    [Fact]
    public async Task DeleteTicket_WhenTicketExists_RemovesTicket()
    {
        await using var db = CreateDbContext();
        var ticket = NewTicket();
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.DeleteTicket(ticket.Id, CancellationToken.None);

        Assert.False(await db.Tickets.AnyAsync(x => x.Id == ticket.Id));
    }

    [Fact]
    public async Task DeleteTicket_WhenTicketDoesNotExist_ThrowsNotFound()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<CommonException.NotFoundException>(
            () => service.DeleteTicket(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("TICKET_NOT_FOUND", exception.MessageCode);
    }

    private static HelpdeskDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
            .UseInMemoryDatabase($"tickets-{Guid.NewGuid()}")
            .Options;

        return new HelpdeskDbContext(options);
    }

    private static TicketService CreateService(HelpdeskDbContext db) =>
        new(db, new TicketCodeGenerator(db));

    private static Ticket NewTicket(TicketStatus status = TicketStatus.Open) => new()
    {
        Id = Guid.NewGuid(),
        Code = $"TCK-{Random.Shared.Next(1, 9999):D4}",
        Title = "VPN lỗi",
        Description = "Không thể kết nối VPN",
        Priority = TicketPriority.Medium,
        Status = status
    };

    private static Label NewLabel(string name, string slug) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Slug = slug,
        Color = "#112233"
    };
}
