using Moq;
using mini_1_helpdesk_ticket.Application.Abstractions.Persistence;
using mini_1_helpdesk_ticket.Application.Abstractions.Services;
using mini_1_helpdesk_ticket.Application.Common.Exceptions;
using mini_1_helpdesk_ticket.Domain.Labels;
using mini_1_helpdesk_ticket.Domain.Tickets;
using TicketApp = mini_1_helpdesk_ticket.Application.Tickets;
using Xunit;

namespace mini_1_helpdesk_ticket.Application.UnitTests.Tickets;

public sealed class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _ticketRepository = new();
    private readonly Mock<ILabelRepository> _labelRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITicketCodeGenerator> _codeGenerator = new();

    [Fact]
    public async Task CreateTicketAsync_CreatesOpenTicket_Saves_AndReturnsDetail()
    {
        Ticket? addedTicket = null;
        _codeGenerator
            .Setup(generator => generator.NextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("TCK-0001");
        _ticketRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<Ticket>(),
                It.IsAny<CancellationToken>()))
            .Callback<Ticket, CancellationToken>((ticket, _) => addedTicket = ticket)
            .Returns(Task.CompletedTask);
        _ticketRepository
            .Setup(repository => repository.GetDetailsByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => addedTicket);

        var result = await CreateService().CreateTicketAsync(
            new TicketApp.Request.CreateTicketRequest
            {
                Title = "  VPN unavailable  ",
                Description = "  Cannot connect  ",
                Priority = TicketPriority.High,
                AssigneeName = "  Alice  "
            });

        Assert.NotNull(addedTicket);
        Assert.Equal("TCK-0001", addedTicket.Code);
        Assert.Equal("VPN unavailable", addedTicket.Title);
        Assert.Equal(TicketStatus.Open, addedTicket.Status);
        Assert.Equal("Alice", addedTicket.AssigneeName);
        Assert.Equal(addedTicket.Id, result.Id);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTicketAsync_WhenTicketDoesNotExist_ThrowsNotFound()
    {
        _ticketRepository
            .Setup(repository => repository.GetDetailsByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            CreateService().GetTicketAsync(Guid.NewGuid()));

        Assert.Equal(ErrorType.NotFound, exception.Type);
        Assert.Equal("TICKET_NOT_FOUND", exception.Code);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetTicketsAsync_WhenPaginationIsInvalid_ThrowsValidation(
        int pageIndex,
        int pageSize)
    {
        var filter = new TicketApp.Request.TicketFilter
        {
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            CreateService().GetTicketsAsync(filter));

        Assert.Equal("PAGINATION_INVALID", exception.Code);
        _ticketRepository.Verify(repository => repository.GetPagedAsync(
            It.IsAny<TicketApp.Request.TicketFilter>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTicketAsync_WithValidVersion_UpdatesAndSaves()
    {
        var ticket = NewTicket();
        _ticketRepository
            .Setup(repository => repository.GetForUpdateByIdAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _ticketRepository
            .Setup(repository => repository.GetDetailsByIdAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var result = await CreateService().UpdateTicketAsync(
            ticket.Id,
            new TicketApp.Request.UpdateTicketRequest
            {
                Title = "Updated title",
                Description = "Updated description",
                Priority = TicketPriority.High,
                Status = TicketStatus.InProgress,
                AssigneeName = "Bob",
                RowVersion = 7
            });

        Assert.Equal("Updated title", result.Title);
        Assert.Equal(TicketStatus.InProgress, result.Status);
        _ticketRepository.Verify(repository => repository.SetExpectedVersion(
            ticket,
            7), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTicketAsync_WhenVersionIsZero_ThrowsBeforeQueryingDatabase()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            CreateService().UpdateTicketAsync(
                Guid.NewGuid(),
                new TicketApp.Request.UpdateTicketRequest { RowVersion = 0 }));

        Assert.Equal("ROW_VERSION_REQUIRED", exception.Code);
        _ticketRepository.Verify(repository => repository.GetForUpdateByIdAsync(
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCommentAsync_AddsCommentAndSaves()
    {
        var ticket = NewTicket();
        _ticketRepository
            .Setup(repository => repository.GetForUpdateByIdAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var result = await CreateService().AddCommentAsync(
            ticket.Id,
            new TicketApp.Request.AddCommentRequest
            {
                AuthorName = "  Alice  ",
                Content = "  Please check VPN.  "
            });

        Assert.Equal(ticket.Id, result.TicketId);
        Assert.Equal("Alice", result.AuthorName);
        Assert.Equal("Please check VPN.", result.Content);
        Assert.Single(ticket.TicketComments);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceLabelsAsync_WhenIdsAreDuplicated_ThrowsBeforeQueryingDatabase()
    {
        var labelId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            CreateService().ReplaceLabelsAsync(
                Guid.NewGuid(),
                new TicketApp.Request.ReplaceTicketLabelsRequest
                {
                    LabelIds = [labelId, labelId]
                }));

        Assert.Equal("LABEL_IDS_INVALID", exception.Code);
        _ticketRepository.Verify(repository => repository.GetForUpdateByIdAsync(
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceLabelsAsync_ReplacesLinksAndSaves()
    {
        var ticket = NewTicket();
        var oldLabel = NewLabel("Old");
        var newLabel = NewLabel("New");
        ticket.TicketLabels.Add(new TicketLabel
        {
            TicketId = ticket.Id,
            LabelId = oldLabel.Id,
            Ticket = ticket,
            Label = oldLabel
        });

        _ticketRepository
            .Setup(repository => repository.GetForUpdateByIdAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _labelRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([newLabel]);

        var result = await CreateService().ReplaceLabelsAsync(
            ticket.Id,
            new TicketApp.Request.ReplaceTicketLabelsRequest
            {
                LabelIds = [newLabel.Id]
            });

        var link = Assert.Single(ticket.TicketLabels);
        Assert.Equal(newLabel.Id, link.LabelId);
        Assert.Equal(newLabel.Id, Assert.Single(result.Labels).Id);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTicketAsync_RemovesTicketAndSaves()
    {
        var ticket = NewTicket();
        _ticketRepository
            .Setup(repository => repository.GetForUpdateByIdAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        await CreateService().DeleteTicketAsync(ticket.Id);

        _ticketRepository.Verify(repository => repository.Remove(ticket), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private TicketApp.Service CreateService() => new(
        _ticketRepository.Object,
        _labelRepository.Object,
        _unitOfWork.Object,
        _codeGenerator.Object);

    private static Ticket NewTicket() => Ticket.Create(
        Guid.NewGuid(),
        "TCK-0001",
        "VPN unavailable",
        "Cannot connect",
        TicketPriority.Medium,
        null);

    private static Label NewLabel(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Slug = name.ToLowerInvariant(),
        Color = "#112233",
        CreatedAt = DateTimeOffset.UtcNow
    };
}
