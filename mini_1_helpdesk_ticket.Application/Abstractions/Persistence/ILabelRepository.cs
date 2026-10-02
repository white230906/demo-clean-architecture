using mini_1_helpdesk_ticket.Domain.Labels;

namespace mini_1_helpdesk_ticket.Application.Abstractions.Persistence;

public interface ILabelRepository
{
    Task<IReadOnlyList<Label>> GetAllAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<Label>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    Task<bool> SlugExistsAsync(
        string slug,
        CancellationToken ct = default);

    Task AddAsync(
        Label label,
        CancellationToken ct = default);

    void RemoveRange(IEnumerable<Label> labels);
}
