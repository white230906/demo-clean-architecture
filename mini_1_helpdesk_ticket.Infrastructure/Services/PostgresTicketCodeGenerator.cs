using Microsoft.EntityFrameworkCore;
using mini_1_helpdesk_ticket.Application.Abstractions.Services;
using mini_1_helpdesk_ticket.Infrastructure.Persistence;

namespace mini_1_helpdesk_ticket.Infrastructure.Services;

public class PostgresTicketCodeGenerator(HelpdeskDbContext dbContext) : ITicketCodeGenerator
{
    private readonly HelpdeskDbContext _dbContext = dbContext;

    public async Task<string> NextAsync(CancellationToken ct = default)
    {
        var values = await _dbContext.Database
            .SqlQueryRaw<long>(
                """SELECT nextval('ticket_code_seq') AS "Value" """)
            .ToListAsync(ct);

        var number = values.Single();

        return $"TCK-{number:D4}";
    }
}