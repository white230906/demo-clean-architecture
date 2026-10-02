using mini_1_helpdesk_ticket.Application.Abstractions.Persistence;
using mini_1_helpdesk_ticket.Application.Abstractions.Services;
using mini_1_helpdesk_ticket.Application.Common.Exceptions;
using mini_1_helpdesk_ticket.Application.Common.Models;
using mini_1_helpdesk_ticket.Domain.Labels;
using mini_1_helpdesk_ticket.Domain.Tickets;

namespace mini_1_helpdesk_ticket.Application.Tickets;

public class Service : IService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILabelRepository _labelRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITicketCodeGenerator _ticketCodeGenerator;

    public Service(
        ITicketRepository ticketRepository,
        ILabelRepository labelRepository,
        IUnitOfWork unitOfWork,
        ITicketCodeGenerator ticketCodeGenerator)
    {
        _ticketRepository = ticketRepository;
        _labelRepository = labelRepository;
        _unitOfWork = unitOfWork;
        _ticketCodeGenerator = ticketCodeGenerator;
    }

    public async Task<Response.TicketDetailResponse> CreateTicketAsync(
        Request.CreateTicketRequest request,
        CancellationToken ct = default)
    {
        var code = await _ticketCodeGenerator.NextAsync(ct);

        var ticket = Ticket.Create(
            Guid.NewGuid(),
            code,
            request.Title,
            request.Description,
            request.Priority,
            request.AssigneeName);

        await _ticketRepository.AddAsync(ticket, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetTicketAsync(ticket.Id, ct);
    }

    public async Task<Response.TicketDetailResponse> GetTicketAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var ticket = await _ticketRepository.GetDetailsByIdAsync(id, ct)
            ?? throw TicketNotFound();

        return MapDetailResponse(ticket);
    }

    public async Task<PaginationResult<Response.TicketListItemResponse>> GetTicketsAsync(
        Request.TicketFilter filter,
        CancellationToken ct = default)
    {
        if (filter.PageIndex < 1 || filter.PageSize is < 1 or > 100)
        {
            throw new AppException(
                ErrorType.Validation,
                "PAGINATION_INVALID",
                "PageIndex phải >= 1 và PageSize phải từ 1 đến 100.");
        }

        var result = await _ticketRepository.GetPagedAsync(filter, ct);
        var items = result.Items.Select(MapListItemResponse).ToList();

        return new PaginationResult<Response.TicketListItemResponse>(
            items,
            result.TotalCount);
    }

    public async Task<Response.TicketDetailResponse> UpdateTicketAsync(
        Guid id,
        Request.UpdateTicketRequest request,
        CancellationToken ct = default)
    {
        if (request.RowVersion == 0)
        {
            throw new AppException(
                ErrorType.Validation,
                "ROW_VERSION_REQUIRED",
                "RowVersion phải lấy từ lần GET gần nhất.");
        }

        var ticket = await _ticketRepository.GetForUpdateByIdAsync(id, ct)
            ?? throw TicketNotFound();

        _ticketRepository.SetExpectedVersion(ticket, request.RowVersion);

        ticket.UpdateDetails(
            request.Title,
            request.Description,
            request.Priority,
            request.Status,
            request.AssigneeName);

        await _unitOfWork.SaveChangesAsync(ct);

        return await GetTicketAsync(id, ct);
    }

    public async Task<Response.CommentResponse> AddCommentAsync(
        Guid ticketId,
        Request.AddCommentRequest request,
        CancellationToken ct = default)
    {
        var ticket = await _ticketRepository.GetForUpdateByIdAsync(ticketId, ct)
            ?? throw TicketNotFound();

        var comment = ticket.AddComment(
            Guid.NewGuid(),
            request.AuthorName,
            request.Content);

        await _unitOfWork.SaveChangesAsync(ct);

        return MapCommentResponse(comment);
    }

    public async Task<Response.TicketLabelsResponse> ReplaceLabelsAsync(
        Guid ticketId,
        Request.ReplaceTicketLabelsRequest request,
        CancellationToken ct = default)
    {
        var labelIds = request.LabelIds ?? [];

        if (labelIds.Count != labelIds.Distinct().Count())
        {
            throw new AppException(
                ErrorType.Validation,
                "LABEL_IDS_INVALID",
                "Danh sách Label ID bị trùng.");
        }

        var ticket = await _ticketRepository.GetForUpdateByIdAsync(ticketId, ct)
            ?? throw TicketNotFound();

        var labels = await _labelRepository.GetByIdsAsync(labelIds, ct);

        if (labels.Count != labelIds.Count)
        {
            throw new AppException(
                ErrorType.Validation,
                "LABEL_IDS_INVALID",
                "Có Label ID không tồn tại.");
        }

        var requestedIds = labelIds.ToHashSet();
        var linksToRemove = ticket.TicketLabels
            .Where(link => !requestedIds.Contains(link.LabelId))
            .ToList();

        foreach (var link in linksToRemove)
        {
            ticket.TicketLabels.Remove(link);
        }

        var currentIds = ticket.TicketLabels
            .Select(link => link.LabelId)
            .ToHashSet();

        foreach (var label in labels.Where(label => !currentIds.Contains(label.Id)))
        {
            ticket.TicketLabels.Add(new TicketLabel
            {
                TicketId = ticket.Id,
                LabelId = label.Id,
                Ticket = ticket,
                Label = label
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return new Response.TicketLabelsResponse
        {
            TicketId = ticket.Id,
            Labels = labels
                .OrderBy(label => label.Name)
                .Select(MapLabelResponse)
                .ToList()
        };
    }

    public async Task DeleteTicketAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var ticket = await _ticketRepository.GetForUpdateByIdAsync(id, ct)
            ?? throw TicketNotFound();

        _ticketRepository.Remove(ticket);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static Response.TicketDetailResponse MapDetailResponse(Ticket ticket)
    {
        return new Response.TicketDetailResponse
        {
            Id = ticket.Id,
            Code = ticket.Code,
            Title = ticket.Title,
            Description = ticket.Description,
            Priority = ticket.Priority,
            Status = ticket.Status,
            AssigneeName = ticket.AssigneeName,
            Comments = ticket.TicketComments
                .OrderBy(comment => comment.CreatedAt)
                .Select(MapCommentResponse)
                .ToList(),
            Labels = ticket.TicketLabels
                .Select(link => link.Label)
                .OrderBy(label => label.Name)
                .Select(MapLabelResponse)
                .ToList(),
            RowVersion = ticket.Version,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt
        };
    }

    private static Response.TicketListItemResponse MapListItemResponse(Ticket ticket)
    {
        return new Response.TicketListItemResponse
        {
            Id = ticket.Id,
            Code = ticket.Code,
            Title = ticket.Title,
            Priority = ticket.Priority,
            Status = ticket.Status,
            AssigneeName = ticket.AssigneeName,
            CommentCount = ticket.TicketComments.Count,
            Labels = ticket.TicketLabels
                .Select(link => link.Label)
                .OrderBy(label => label.Name)
                .Select(MapLabelResponse)
                .ToList(),
            RowVersion = ticket.Version,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt
        };
    }

    private static Response.CommentResponse MapCommentResponse(
        TicketComment comment)
    {
        return new Response.CommentResponse
        {
            Id = comment.Id,
            TicketId = comment.TicketId,
            AuthorName = comment.AuthorName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };
    }

    private static Labels.Response.LabelResponse MapLabelResponse(Label label)
    {
        return new Labels.Response.LabelResponse
        {
            Id = label.Id,
            Name = label.Name,
            Slug = label.Slug,
            Color = label.Color,
            CreatedAt = label.CreatedAt
        };
    }

    private static AppException TicketNotFound()
    {
        return new AppException(
            ErrorType.NotFound,
            "TICKET_NOT_FOUND",
            "Không tìm thấy ticket.");
    }
}
