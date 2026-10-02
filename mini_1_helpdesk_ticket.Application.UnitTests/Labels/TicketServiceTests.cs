using Moq;
using mini_1_helpdesk_ticket.Application.Abstractions.Persistence;
using mini_1_helpdesk_ticket.Application.Common.Exceptions;
using mini_1_helpdesk_ticket.Domain.Labels;
using LabelApp = mini_1_helpdesk_ticket.Application.Labels;
using Xunit;

namespace mini_1_helpdesk_ticket.Application.UnitTests.Labels;

public sealed class LabelServiceTests
{
    private readonly Mock<ILabelRepository> _labelRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task GetLabelsAsync_MapsRepositoryEntitiesToResponses()
    {
        var label = NewLabel("Network", "network", "#112233");
        _labelRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([label]);

        var result = await CreateService().GetLabelsAsync();

        var response = Assert.Single(result);
        Assert.Equal(label.Id, response.Id);
        Assert.Equal("Network", response.Name);
        Assert.Equal("network", response.Slug);
    }

    [Fact]
    public async Task CreateLabelAsync_TrimsInput_GeneratesUniqueSlug_AndSaves()
    {
        _labelRepository
            .SetupSequence(repository => repository.SlugExistsAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        Label? addedLabel = null;
        _labelRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<Label>(),
                It.IsAny<CancellationToken>()))
            .Callback<Label, CancellationToken>((label, _) => addedLabel = label)
            .Returns(Task.CompletedTask);

        var result = await CreateService().CreateLabelAsync(
            new LabelApp.Request.CreateLabelRequest
            {
                Name = "  Mạng nội bộ  ",
                Color = "  #A1B2C3  "
            });

        Assert.NotNull(addedLabel);
        Assert.Equal("Mạng nội bộ", addedLabel.Name);
        Assert.Equal("mang-noi-bo-2", addedLabel.Slug);
        Assert.Equal("#A1B2C3", addedLabel.Color);
        Assert.Equal(addedLabel.Id, result.Id);

        _labelRepository.Verify(repository => repository.SlugExistsAsync(
            "mang-noi-bo",
            It.IsAny<CancellationToken>()), Times.Once);
        _labelRepository.Verify(repository => repository.SlugExistsAsync(
            "mang-noi-bo-2",
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateLabelAsync_WhenColorIsInvalid_ThrowsValidation_WithoutSaving()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            CreateService().CreateLabelAsync(
                new LabelApp.Request.CreateLabelRequest
                {
                    Name = "Network",
                    Color = "red"
                }));

        Assert.Equal(ErrorType.Validation, exception.Type);
        Assert.Equal("LABEL_COLOR_INVALID", exception.Code);
        _labelRepository.Verify(repository => repository.AddAsync(
            It.IsAny<Label>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteLabelsAsync_RemovesExistingLabelsOnce_AndSaves()
    {
        var first = NewLabel("Network", "network", "#112233");
        var second = NewLabel("Hardware", "hardware", "#445566");
        var requestIds = new[] { first.Id, second.Id, first.Id };

        _labelRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);

        var deletedCount = await CreateService().DeleteLabelsAsync(requestIds);

        Assert.Equal(2, deletedCount);
        _labelRepository.Verify(repository => repository.GetByIdsAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
        _labelRepository.Verify(repository => repository.RemoveRange(
            It.Is<IEnumerable<Label>>(labels => labels.Count() == 2)), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLabelsAsync_WhenAnIdDoesNotExist_ThrowsWithoutSaving()
    {
        var existing = NewLabel("Network", "network", "#112233");
        var missingId = Guid.NewGuid();

        _labelRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            CreateService().DeleteLabelsAsync([existing.Id, missingId]));

        Assert.Equal("LABEL_IDS_INVALID", exception.Code);
        Assert.NotNull(exception.Details);
        _labelRepository.Verify(repository => repository.RemoveRange(
            It.IsAny<IEnumerable<Label>>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private LabelApp.Service CreateService() =>
        new(_labelRepository.Object, _unitOfWork.Object);

    private static Label NewLabel(string name, string slug, string color) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Slug = slug,
        Color = color,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
