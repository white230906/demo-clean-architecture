using mini_1_helpdesk_ticket.Application.Common.Models;
using mini_1_helpdesk_ticket.Application.Tickets;
using mini_1_helpdesk_ticket.Domain.Tickets;

namespace mini_1_helpdesk_ticket.Application.Abstractions.Persistence;

public interface ITicketRepository
{
    Task<Ticket?> GetForUpdateByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<Ticket?> GetDetailsByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<PaginationResult<Ticket>> GetPagedAsync(
        Request.TicketFilter filter,
        CancellationToken ct = default);

    Task AddAsync(
        Ticket ticket,
        CancellationToken ct = default);

    void SetExpectedVersion(Ticket ticket, uint expectedVersion);

    void Remove(Ticket ticket);
}
