using Microsoft.EntityFrameworkCore;
using mini_1_helpdesk_ticket.Repo;
using mini_1_helpdesk_ticket.Repo.Entity;
using mini_1_helpdesk_ticket.Service.Exceptions;
using mini_1_helpdesk_ticket.Service.Utils;
using Npgsql;
using StackExchange.Redis;

namespace mini_1_helpdesk_ticket.Service.Labels;

public class Service: IService
{
    private readonly HelpdeskDbContext _dbContext;
    
    public Service(HelpdeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<IReadOnlyList<Response.LabelResponse>> GetLabels(CancellationToken ct)
    {

        var labels = _dbContext.Labels
            .AsNoTracking()
            .Where(x => x.IsDeleted == false);
        
        var sortLabels = labels.OrderBy(x => x.CreatedAt);

        var select = sortLabels.Select(x => new Response.LabelResponse()
        {
            Id = x.Id,
            Name = x.Name,
            Slug =  x.Slug,
            Color =  x.Color,
            CreatedAt = x.CreatedAt,
        });
        
        var result = await select.ToListAsync(ct);
        return result;
    }

    public async Task<Response.LabelResponse> CreateLabel(Request.CreateLabelRequest request, CancellationToken ct)
    {
        var name = TextRules.Require(request.Name, nameof(request.Name));
        var slug = TextRules.ToSlug(name);
        
        var label = new Label()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Color = request.Color.Trim(),
        };
        
        _dbContext.Labels.Add(label);

        for (int i = 2;; i++)
        {
            try
            {
                await _dbContext.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is PostgresException
                      {
                          SqlState: PostgresErrorCodes.UniqueViolation,
                          ConstraintName: "ux_labels_slug"
                      })
            {
                slug = $"{slug}-{i}";
            }
            label.Slug = slug;
        }

        return new Response.LabelResponse
        {
            Id = label.Id,
            Name = label.Name,
            Slug = label.Slug,
            Color = label.Color,
            CreatedAt = label.CreatedAt,
        };
    }

    public async Task<string> DeleteLabels(List<Guid> labelIds, CancellationToken ct)
    {
        var deleteLabels = _dbContext.Labels
            .Where(x => labelIds.Contains(x.Id));
        
        _dbContext.Labels.RemoveRange(deleteLabels);

        await _dbContext.SaveChangesAsync(ct);

        var result = labelIds.Count - deleteLabels.Count();

        return $"Xóa {result} phần tử";
    }
}