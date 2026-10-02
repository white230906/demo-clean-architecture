using Microsoft.EntityFrameworkCore;
using mini_1_helpdesk_ticket.Application.Abstractions.Persistence;
using mini_1_helpdesk_ticket.Application.Common.Models;
using mini_1_helpdesk_ticket.Application.Tickets;
using mini_1_helpdesk_ticket.Domain.Tickets;
using mini_1_helpdesk_ticket.Infrastructure.Persistence;

namespace mini_1_helpdesk_ticket.Infrastructure.Repositories;

public class TicketRepository: ITicketRepository
{
    private readonly HelpdeskDbContext _dbContext;

      public TicketRepository(HelpdeskDbContext dbContext)
      {
          _dbContext = dbContext;
      }

      public async Task<Ticket?> GetForUpdateByIdAsync(
          Guid id,
          CancellationToken ct = default)
      {
          return await _dbContext.Tickets
              .Include(ticket => ticket.TicketLabels)
              .SingleOrDefaultAsync(ticket => ticket.Id == id, ct);
      }

      public async Task<Ticket?> GetDetailsByIdAsync(
          Guid id,
          CancellationToken ct = default)
      {
          return await _dbContext.Tickets
              .AsNoTracking()
              .AsSplitQuery()
              .Include(ticket => ticket.TicketComments)
              .Include(ticket => ticket.TicketLabels)
                  .ThenInclude(ticketLabel => ticketLabel.Label)
              .SingleOrDefaultAsync(ticket => ticket.Id == id, ct);
      }

      public async Task<PaginationResult<Ticket>> GetPagedAsync(
          Request.TicketFilter filter,
          CancellationToken ct = default)
      {
          var query = _dbContext.Tickets
              .AsNoTracking()
              .AsQueryable();

          if (filter.Status.HasValue)
          {
              query = query.Where(ticket =>
                  ticket.Status == filter.Status.Value);
          }

          if (filter.Priority.HasValue)
          {
              query = query.Where(ticket =>
                  ticket.Priority == filter.Priority.Value);
          }

          if (!string.IsNullOrWhiteSpace(filter.Assignee))
          {
              var pattern = $"%{filter.Assignee.Trim()}%";

              query = query.Where(ticket =>
                  ticket.AssigneeName != null &&
                  EF.Functions.ILike(ticket.AssigneeName, pattern));
          }

          if (!string.IsNullOrWhiteSpace(filter.Q))
          {
              var pattern = $"%{filter.Q.Trim()}%";

              query = query.Where(ticket =>
                  EF.Functions.ILike(ticket.Code, pattern) ||
                  EF.Functions.ILike(ticket.Title, pattern) ||
                  EF.Functions.ILike(ticket.Description, pattern));
          }

          if (filter.LabelId.HasValue &&
              filter.LabelId.Value != Guid.Empty)
          {
              var labelId = filter.LabelId.Value;

              query = query.Where(ticket =>
                  ticket.TicketLabels.Any(link =>
                      link.LabelId == labelId));
          }

          var totalCount = await query.CountAsync(ct);

          var tickets = await query
              .Include(ticket => ticket.TicketComments)
              .Include(ticket => ticket.TicketLabels)
                  .ThenInclude(ticketLabel => ticketLabel.Label)
              .AsSplitQuery()
              .OrderByDescending(ticket => ticket.CreatedAt)
              .ThenByDescending(ticket => ticket.Id)
              .Skip((filter.PageIndex - 1) * filter.PageSize)
              .Take(filter.PageSize)
              .ToListAsync(ct);

          return new PaginationResult<Ticket>(
              tickets,
              totalCount);
      }

      public async Task AddAsync(
          Ticket ticket,
          CancellationToken ct = default)
      {
          await _dbContext.Tickets.AddAsync(ticket, ct);
      }

      public void SetExpectedVersion(
          Ticket ticket,
          uint expectedVersion)
      {
          _dbContext.Entry(ticket)
              .Property(entity => entity.Version)
              .OriginalValue = expectedVersion;
      }

      public void Remove(Ticket ticket)
      {
          ticket.IsDeleted = true;

          _dbContext.Entry(ticket)
              .Property(entity => entity.IsDeleted)
              .IsModified = true;
      }
}