using Microsoft.EntityFrameworkCore;
using mini_1_helpdesk_ticket.Application.Abstractions.Persistence;
using mini_1_helpdesk_ticket.Domain.Labels;
using mini_1_helpdesk_ticket.Infrastructure.Persistence;

namespace mini_1_helpdesk_ticket.Infrastructure.Repositories;

public class LabelRepository: ILabelRepository
{
    private readonly HelpdeskDbContext _dbContext;

    public LabelRepository(HelpdeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Label>> GetAllAsync(
        CancellationToken ct = default)
    {
        return await _dbContext.Labels
            .AsNoTracking()
            .OrderBy(label => label.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Label>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        return await _dbContext.Labels
            .Where(label => ids.Contains(label.Id))
            .OrderBy(label => label.Name)
            .ToListAsync(ct);
    }

    public async Task<bool> SlugExistsAsync(
        string slug,
        CancellationToken ct = default)
    {
        return await _dbContext.Labels
            .IgnoreQueryFilters()
            .AnyAsync(label => label.Slug == slug, ct);
    }

    public async Task AddAsync(
        Label label,
        CancellationToken ct = default)
    {
        await _dbContext.Labels.AddAsync(label, ct);
    }

    public void RemoveRange(IEnumerable<Label> labels)
    {
        foreach (var label in labels)
        {
            label.IsDeleted = true;

            _dbContext.Entry(label)
                .Property(x => x.IsDeleted)
                .IsModified = true;
        }
    }
}