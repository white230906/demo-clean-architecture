using Microsoft.EntityFrameworkCore;
using mini_1_helpdesk_ticket.Repo;
using mini_1_helpdesk_ticket.Repo.Entity;
using mini_1_helpdesk_ticket.Repo.Enum;
using mini_1_helpdesk_ticket.Service.Exceptions;
using mini_1_helpdesk_ticket.Service.Models;
using mini_1_helpdesk_ticket.Service.Utils;

namespace mini_1_helpdesk_ticket.Service.Tickets;

public class Service: IService
{
    private readonly HelpdeskDbContext _dbContext;
    private readonly TicketCodeGenerator _codes;

    public Service(HelpdeskDbContext dbContext, TicketCodeGenerator codes)
    {
        _dbContext = dbContext;
        _codes = codes;
    }
    
    public async Task<Response.TicketDetailResponse> CreateTicket(Request.CreateTicketRequest request, CancellationToken ct)
    {
        var newTicket = new Ticket
        {
            Id = Guid.NewGuid(),
            Code = await _codes.NextAsync(ct),
            Title = TextRules.Require(request.Title, nameof(request.Title)),
            Description = TextRules.Require(request.Description, nameof(request.Description)),
            Priority = request.Priority,
            Status = TicketStatus.Open,
            AssigneeName = TextRules.NullIfWhiteSpace(request.AssigneeName),
        };
        
        await _dbContext.Tickets.AddAsync(newTicket, ct);
        await _dbContext.SaveChangesAsync(ct);
        
        return await GetTicket(newTicket.Id, ct);
    }

    public async Task<Response.TicketDetailResponse> GetTicket(Guid id, CancellationToken ct)
    {
        var ticket = _dbContext.Tickets
            .Where(t => t.Id == id && !t.IsDeleted);

        var mapIntoResponse = ticket.Select(x => new Response.TicketDetailResponse()
        {
            Id = x.Id,
            Code = x.Code,
            Title = x.Title,
            Description = x.Description,
            Priority = x.Priority,
            AssigneeName = x.AssigneeName,
            Status = x.Status,
            RowVersion = EF.Property<uint>(x, "xmin"),
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            
            Comments = x.TicketComments
                .OrderBy(t => t.CreatedAt)
                .Select(t => new Response.CommentResponse()
                {
                    Id = t.Id,
                    TicketId = t.TicketId,
                    AuthorName = t.AuthorName,
                    Content = t.Content,
                    CreatedAt = t.CreatedAt
                }).ToList(),
            
            Labels = x.TicketLabels
                .OrderBy(l => l.Label.Name)
                .Select(l => new Labels.Response.LabelResponse()
                {
                    Id = l.LabelId,
                    Name = l.Label.Name,
                    Slug = l.Label.Slug,
                    Color = l.Label.Color,
                    CreatedAt = l.Label.CreatedAt,
                }).ToList()
        });
        
        var result = await mapIntoResponse.SingleOrDefaultAsync(ct);

        return result ?? throw new CommonException.NotFoundException(
            "TICKET_NOT_FOUND", "Không tìm thấy ticket");
    }

    public async Task<BasePaginationResponse> GetTickets(Request.TicketFilter filter, CancellationToken ct)
    {
        if (filter.PageIndex < 1 || filter.PageSize is < 1 or > 100)
        {
            throw new CommonException.BadRequestException(
                "VALIDATION_FAILED",
                "PageIndex phải >= 1 và pageSize phải từ 1 đến 100.");
        }
        
        IQueryable<Ticket> query = _dbContext.Tickets.AsNoTracking();

        if (filter.Status is { } status)
            // = if(filter.Status.HasValue){var status = filter.Status.Value}
        {
            query = query.Where(t => t.Status == status);
        }

        if (filter.Priority is { } priority)
        {
            query = query.Where(t => t.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(filter.Assignee))
        {
            var assignPattern = $"%{filter.Assignee.Trim()}%";
            query = query.Where(x =>
                x.AssigneeName != null &&
                EF.Functions.ILike(x.AssigneeName, assignPattern));
        }

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var searchPattern = $"%{filter.Q.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Code, searchPattern) ||
                EF.Functions.ILike(x.Description, searchPattern) ||
                EF.Functions.ILike(x.Title, searchPattern));
            //partern matching này nó khá giống với ToLower.Containt
            //Sự khác biệt chỉ là vì có ToLower, nên nó phải mất thời gian
            //đi lower cái column đó rồi nó mới tìm
            //còn partern thì nó ánh xạ xuống postgres và tìm kiếm luôn
        }

        if (filter.LabelId is { } labelId && labelId != Guid.Empty)
        {
            query = query.Where(x => 
                x.TicketLabels.Any(l => l.LabelId == labelId));
        }
        
        var totalCount = await query.CountAsync(ct);

        var items = query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize);
        
        var result = await items.Select(x => new Response.TicketListItemResponse()
        {
            Id = x.Id,
            Code = x.Code,
            Title = x.Title,
            Priority = x.Priority,
            AssigneeName = x.AssigneeName,
            Status = x.Status,
            CommentCount = x.TicketComments.Count(),
            
            Labels = x.TicketLabels
                .OrderBy(l => l.Label.Name)
                .Select(l => new Labels.Response.LabelResponse()
                {
                    Id = l.LabelId,
                    Color = l.Label.Color,
                    Name = l.Label.Name,
                    Slug = l.Label.Slug,
                    CreatedAt = l.Label.CreatedAt,
                }).ToList(),
            RowVersion = EF.Property<uint>(x, "xmin"),
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            
        }).ToListAsync(ct);
        
        return ApiResponseFactory
            .BasePagination(result, filter.PageIndex, filter.PageSize, totalCount);
    }

    public async Task<Response.TicketDetailResponse> UpdateTicket(Guid id, Request.UpdateTicketRequest request, CancellationToken ct)
    {
        //> id chỉ đảm bảo chúng ta update đúng ticket, nhưng không đảm bảo dữ liệu client đang sửa là phiên bản mới nhất. xmin được dùng làm optimistic
        //> concurrency token để ngăn hai request vô tình ghi đè lẫn nhau. Nếu ticket đã bị request khác thay đổi sau lần GET của client, xmin không còn khớp,
        //> update tác động 0 dòng và hệ thống trả về 409 Conflict.
        //ngăn chặn khi người dùng update dữ liệu đã cũ

        if (request.RowVersion == 0)
        {
            throw new CommonException.BadRequestException(
                "VALIDATION_FAILED",
                "RowVersion phải lấy từ GET gần nhất");
        }

        var ticket = await _dbContext.Tickets
                         .SingleOrDefaultAsync(t => t.Id == id, ct)
                     ?? throw new CommonException.BadRequestException(
                         "TICKET_NOT_FOUND",
                         "Không tìm thấy ticket");
        
        //this row will check that the rowversion is origin value of xmin or not
        _dbContext.Entry(ticket)
            .Property("xmin")
            .OriginalValue = request.RowVersion;
        
        ticket.Title = TextRules.Require(request.Title, nameof(request.Title));
        ticket.Description = TextRules.Require(
            request.Description, nameof(request.Description));
        ticket.Priority = request.Priority;
        ticket.Status = request.Status;
        ticket.AssigneeName = TextRules
            .NullIfWhiteSpace(request.AssigneeName);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CommonException.ConflictException(
                "TICKET_CONCURRENCY_CONFLICT",
                "Ticket đã được request khác cập nhật. Hãy get lại dữ liệu mới nhất");
        }

        return await GetTicket(id, ct);
    }

    public async Task<Response.CommentResponse> AddCommentTicket(Guid ticketId, Request.AddCommentRequest request, CancellationToken ct)
    {
        var ticket = await _dbContext.Tickets
                         .SingleOrDefaultAsync(x => x.Id == ticketId, ct)
                     ?? throw new CommonException.NotFoundException(
                         "TICKET_NOT_FOUND",
                         "Không tìm thấy tickets");

        if (ticket.Status == TicketStatus.Closed)
        {
            throw new CommonException.ConflictException(
                "TICKET_CLOSED",
                "Ticket đã đóng và không nhận comment mới");
        }

        var newComment = new TicketComment()
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorName = TextRules.Require(request.AuthorName, nameof(request.AuthorName)),
            Content = TextRules.Require(request.Content, nameof(request.Content)),
        };
        
        _dbContext.TicketComments.Add(newComment);
        await _dbContext.SaveChangesAsync(ct);

        return new Response.CommentResponse()
        {
            Id = newComment.Id,
            TicketId = newComment.TicketId,
            AuthorName = newComment.AuthorName,
            Content = newComment.Content,
            CreatedAt = newComment.CreatedAt,
        };
    }

    public async Task<Response.TicketLabelsResponse> ReplaceLabelsTicket(
        Guid ticketId, 
        Request.ReplaceTicketLabelsRequest request, 
        CancellationToken ct)
    {
        var labelIds = request.LabelIds ?? Array.Empty<Guid>();

        if (labelIds.Count != labelIds.Distinct().Count())
        {
            throw new CommonException.BadRequestException(
                "LABEL_IDS_INVALID",
                "Danh sách label có ID bị trùng.");
        }

        var ticket = await _dbContext.Tickets
                         .Include(x => x.TicketLabels)
                         .SingleOrDefaultAsync(x => x.Id == ticketId, ct)
                     ?? throw new CommonException.NotFoundException(
                         "TICKET_NOT_FOUND",
                         "Không tìm thấy ticket");
        
        var labels = await _dbContext.Labels
            .Where(x => labelIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        if (labels.Count != labelIds.Count)
        {
            throw new CommonException.BadRequestException(
                "LABEL_IDS_INVALID",
                "Có label ID không tồn tại");
        }

        var requestedIds = labelIds.ToHashSet();
        var currentIds = ticket.TicketLabels
            .Select(x => x.LabelId)
            .ToHashSet();
        
        var linksToRemove = ticket.TicketLabels
            .Where(x => !requestedIds.Contains(x.LabelId))
            .ToList();

        var linksToAdd = requestedIds
            .Where(labelId => !currentIds.Contains(labelId))
            .Select(labelId => new TicketLabel()
            {
                TicketId = ticket.Id,
                LabelId = labelId,
            });
        
        _dbContext.TicketLabels.RemoveRange(linksToRemove);
        _dbContext.TicketLabels.AddRange(linksToAdd);
        
        await _dbContext.SaveChangesAsync(ct);

        var responseLabels = labels
            .Select(x => new Labels.Response.LabelResponse()
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                Color = x.Color,
                CreatedAt = x.CreatedAt,
            }).ToList();

        return new Response.TicketLabelsResponse()
        {
            TicketId = ticket.Id,
            Labels = responseLabels,
        };
    }

    public async Task DeleteTicket(Guid id, CancellationToken ct)
    {
        var ticket = await _dbContext.Tickets
                         .SingleOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new CommonException.NotFoundException(
                         "TICKET_NOT_FOUND",
                         "Không tìm thấy ticket");
        
        _dbContext.Tickets.Remove(ticket);
        await _dbContext.SaveChangesAsync(ct);
    }
}