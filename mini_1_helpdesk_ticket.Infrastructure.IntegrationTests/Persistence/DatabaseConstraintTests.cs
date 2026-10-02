using Microsoft.EntityFrameworkCore;
using mini_1_helpdesk_ticket.Domain.Labels;
using Xunit;

namespace mini_1_helpdesk_ticket.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class DatabaseConstraintTests
{
    private readonly PostgreSqlFixture _database;

    public DatabaseConstraintTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task SaveChangesAsync_WhenLabelColorIsInvalid_ThrowsDatabaseError()
    {
        await using var dbContext = _database.CreateDbContext();
        dbContext.Labels.Add(NewLabel(
            $"Invalid color {Guid.NewGuid():N}",
            $"invalid-color-{Guid.NewGuid():N}",
            "red"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_WhenLabelSlugIsDuplicated_ThrowsDatabaseError()
    {
        await using var dbContext = _database.CreateDbContext();
        var slug = $"duplicate-{Guid.NewGuid():N}";

        dbContext.Labels.AddRange(
            NewLabel("First label", slug, "#112233"),
            NewLabel("Second label", slug, "#445566"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Labels_QueryFilter_HidesSoftDeletedRows()
    {
        var label = NewLabel(
            "Deleted label",
            $"deleted-{Guid.NewGuid():N}",
            "#112233");
        label.IsDeleted = true;

        await using (var writeContext = _database.CreateDbContext())
        {
            writeContext.Labels.Add(label);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = _database.CreateDbContext();

        Assert.Null(await readContext.Labels.SingleOrDefaultAsync(x => x.Id == label.Id));
        Assert.NotNull(await readContext.Labels
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == label.Id));
    }

    private static Label NewLabel(string name, string slug, string color) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Slug = slug,
        Color = color
    };
}
