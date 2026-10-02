using Microsoft.EntityFrameworkCore;
using mini_1_helpdesk_ticket.Application.Common.Exceptions;
using mini_1_helpdesk_ticket.Domain.Tickets;
using Xunit;

namespace mini_1_helpdesk_ticket.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class TicketConcurrencyTests
{
    private readonly PostgreSqlFixture _database;

    public TicketConcurrencyTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task SaveChangesAsync_WhenAnotherRequestAlreadyUpdatedTicket_ThrowsConflict()
    {
        var ticket = Ticket.Create(
            Guid.NewGuid(),
            $"TCK-{Guid.NewGuid():N}"[..20],
            "VPN unavailable",
            "Cannot connect to VPN",
            TicketPriority.Medium,
            null);

        await using (var seedContext = _database.CreateDbContext())
        {
            seedContext.Tickets.Add(ticket);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = _database.CreateDbContext();
        await using var secondContext = _database.CreateDbContext();
        var firstCopy = await firstContext.Tickets.SingleAsync(x => x.Id == ticket.Id);
        var staleCopy = await secondContext.Tickets.SingleAsync(x => x.Id == ticket.Id);

        firstCopy.UpdateDetails(
            "Updated by first request",
            firstCopy.Description,
            firstCopy.Priority,
            firstCopy.Status,
            firstCopy.AssigneeName);
        await firstContext.SaveChangesAsync();

        staleCopy.UpdateDetails(
            "Updated by stale request",
            staleCopy.Description,
            staleCopy.Priority,
            staleCopy.Status,
            staleCopy.AssigneeName);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            secondContext.SaveChangesAsync());

        Assert.Equal(ErrorType.Conflict, exception.Type);
        Assert.Equal("TICKET_CONCURRENCY_CONFLICT", exception.Code);

        await using var verifyContext = _database.CreateDbContext();
        var savedTicket = await verifyContext.Tickets
            .AsNoTracking()
            .SingleAsync(x => x.Id == ticket.Id);
        Assert.Equal("Updated by first request", savedTicket.Title);
    }
}
