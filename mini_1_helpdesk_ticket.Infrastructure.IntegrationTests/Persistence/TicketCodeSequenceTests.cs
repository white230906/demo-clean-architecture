using mini_1_helpdesk_ticket.Infrastructure.Services;
using Xunit;

namespace mini_1_helpdesk_ticket.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class TicketCodeSequenceTests
{
    private readonly PostgreSqlFixture _database;

    public TicketCodeSequenceTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task NextAsync_ReturnsFormattedIncreasingCodes()
    {
        await using var firstContext = _database.CreateDbContext();
        await using var secondContext = _database.CreateDbContext();
        var firstGenerator = new PostgresTicketCodeGenerator(firstContext);
        var secondGenerator = new PostgresTicketCodeGenerator(secondContext);

        var firstCode = await firstGenerator.NextAsync();
        var secondCode = await secondGenerator.NextAsync();

        Assert.Matches("^TCK-[0-9]{4,}$", firstCode);
        Assert.Matches("^TCK-[0-9]{4,}$", secondCode);

        var firstNumber = long.Parse(firstCode[4..]);
        var secondNumber = long.Parse(secondCode[4..]);
        Assert.Equal(firstNumber + 1, secondNumber);
    }
}
