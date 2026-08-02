using Microsoft.EntityFrameworkCore;

namespace mini_1_helpdesk_ticket.Repo;

public class TicketCodeGenerator
{
    private readonly HelpdeskDbContext _dbContext;
    public TicketCodeGenerator(HelpdeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

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