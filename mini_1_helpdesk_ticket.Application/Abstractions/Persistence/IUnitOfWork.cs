namespace mini_1_helpdesk_ticket.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(
        CancellationToken ct = default);
}
