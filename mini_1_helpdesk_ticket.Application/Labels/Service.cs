using System.Text.RegularExpressions;
using mini_1_helpdesk_ticket.Application.Abstractions.Persistence;
using mini_1_helpdesk_ticket.Application.Common;
using mini_1_helpdesk_ticket.Application.Common.Exceptions;
using mini_1_helpdesk_ticket.Domain.Labels;

namespace mini_1_helpdesk_ticket.Application.Labels;

public sealed class Service : IService
{
    private readonly ILabelRepository _labelRepository;
    private readonly IUnitOfWork _unitOfWork;

    public Service(
        ILabelRepository labelRepository,
        IUnitOfWork unitOfWork)
    {
        _labelRepository = labelRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<Response.LabelResponse>> GetLabelsAsync(
        CancellationToken ct = default)
    {
        var labels = await _labelRepository.GetAllAsync(ct);

        return labels
            .Select(MapResponse)
            .ToList();
    }

    public async Task<Response.LabelResponse> CreateLabelAsync(
        Request.CreateLabelRequest request,
        CancellationToken ct = default)
    {
        var name = TextRules.Require(request.Name, nameof(request.Name));
        var color = TextRules.Require(request.Color, nameof(request.Color));

        if (name.Length > 100)
        {
            throw new AppException(
                ErrorType.Validation,
                "LABEL_NAME_TOO_LONG",
                "Name không được vượt quá 100 ký tự.");
        }

        if (!Regex.IsMatch(color, "^#[0-9A-Fa-f]{6}$"))
        {
            throw new AppException(
                ErrorType.Validation,
                "LABEL_COLOR_INVALID",
                "Color phải có định dạng #RRGGBB.");
        }

        var baseSlug = TextRules.ToSlug(name);
        var slug = baseSlug;

        for (var suffix = 2; await _labelRepository.SlugExistsAsync(slug, ct); suffix++)
        {
            slug = $"{baseSlug}-{suffix}";
        }

        var label = new Label
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Color = color
        };

        await _labelRepository.AddAsync(label, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapResponse(label);
    }

    public async Task<int> DeleteLabelsAsync(
        IReadOnlyCollection<Guid> labelIds,
        CancellationToken ct = default)
    {
        if (labelIds.Count == 0 || labelIds.Any(id => id == Guid.Empty))
        {
            throw new AppException(
                ErrorType.Validation,
                "LABEL_IDS_INVALID",
                "Danh sách Label ID không hợp lệ.");
        }

        var distinctIds = labelIds.Distinct().ToArray();
        var labels = await _labelRepository.GetByIdsAsync(distinctIds, ct);

        if (labels.Count != distinctIds.Length)
        {
            var foundIds = labels.Select(label => label.Id).ToHashSet();
            var missingIds = distinctIds
                .Where(id => !foundIds.Contains(id))
                .ToArray();

            throw new AppException(
                ErrorType.Validation,
                "LABEL_IDS_INVALID",
                "Có Label ID không tồn tại.",
                new { MissingIds = missingIds });
        }

        _labelRepository.RemoveRange(labels);
        await _unitOfWork.SaveChangesAsync(ct);

        return labels.Count;
    }

    private static Response.LabelResponse MapResponse(Label label)
    {
        return new Response.LabelResponse
        {
            Id = label.Id,
            Name = label.Name,
            Slug = label.Slug,
            Color = label.Color,
            CreatedAt = label.CreatedAt
        };
    }
}
