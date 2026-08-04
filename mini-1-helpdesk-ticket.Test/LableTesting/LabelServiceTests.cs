using Microsoft.EntityFrameworkCore;
using mini_1_helpdesk_ticket.Repo;
using mini_1_helpdesk_ticket.Repo.Entity;
using mini_1_helpdesk_ticket.Service.Exceptions;
using LabelRequest = mini_1_helpdesk_ticket.Service.Labels.Request;
using LabelService = mini_1_helpdesk_ticket.Service.Labels.Service;
using Xunit;

namespace mini_1_helpdesk_ticket.Test.LableTesting;

public sealed class LabelServiceTests
{
    [Fact]
    public async Task GetLabels_ReturnsOnlyActiveLabels()
    {
        await using var db = CreateDbContext();
        db.Labels.AddRange(
            NewLabel("Network", "network", "#112233"),
            NewLabel("Deleted", "deleted", "#445566", isDeleted: true));
        await db.SaveChangesAsync();

        var service = new LabelService(db);

        var result = await service.GetLabels(CancellationToken.None);

        var label = Assert.Single(result);
        Assert.Equal("Network", label.Name);
        Assert.Equal("network", label.Slug);
    }

    [Fact]
    public async Task CreateLabel_TrimsNameAndGeneratesVietnameseSlug()
    {
        await using var db = CreateDbContext();
        var service = new LabelService(db);
        var request = new LabelRequest.CreateLabelRequest
        {
            Name = "  Mạng nội bộ  ",
            Color = "#A1B2C3"
        };

        var result = await service.CreateLabel(request, CancellationToken.None);

        Assert.Equal("Mạng nội bộ", result.Name);
        Assert.Equal("mang-noi-bo", result.Slug);
        Assert.Equal("#A1B2C3", result.Color);
        Assert.NotEqual(default, result.CreatedAt);

        var saved = await db.Labels.SingleAsync();
        Assert.Equal(result.Id, saved.Id);
        Assert.Equal(result.Slug, saved.Slug);
    }

    [Fact]
    public async Task CreateLabel_WhenNameIsBlank_ThrowsBadRequest()
    {
        await using var db = CreateDbContext();
        var service = new LabelService(db);
        var request = new LabelRequest.CreateLabelRequest
        {
            Name = "   ",
            Color = "#A1B2C3"
        };

        var exception = await Assert.ThrowsAsync<CommonException.BadRequestException>(
            () => service.CreateLabel(request, CancellationToken.None));

        Assert.Equal("VALIDATION_FAILED", exception.MessageCode);
        Assert.Empty(db.Labels);
    }

    [Fact]
    public async Task DeleteLabels_RemovesMatchingLabels()
    {
        await using var db = CreateDbContext();
        var first = NewLabel("Network", "network", "#112233");
        var second = NewLabel("Hardware", "hardware", "#445566");
        db.Labels.AddRange(first, second);
        await db.SaveChangesAsync();

        var service = new LabelService(db);

        var result = await service.DeleteLabels(
            new List<Guid> { first.Id, second.Id },
            CancellationToken.None);

        Assert.Equal("Xóa 2 phần tử", result);
        Assert.Empty(await db.Labels.ToListAsync());
    }

    private static HelpdeskDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
            .UseInMemoryDatabase($"labels-{Guid.NewGuid()}")
            .Options;

        return new HelpdeskDbContext(options);
    }

    private static Label NewLabel(
        string name,
        string slug,
        string color,
        bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Slug = slug,
        Color = color,
        IsDeleted = isDeleted
    };
}
