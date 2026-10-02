namespace mini_1_helpdesk_ticket.Application.Abstractions.Services;

public interface ITicketCodeGenerator
{
    Task<string> NextAsync(CancellationToken ct = default);
}