using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using mini_1_helpdesk_ticket.Application.Abstractions.Persistence;
using mini_1_helpdesk_ticket.Application.Abstractions.Services;
using mini_1_helpdesk_ticket.Infrastructure.Persistence;
using mini_1_helpdesk_ticket.Infrastructure.Repositories;
using mini_1_helpdesk_ticket.Infrastructure.Services;

namespace mini_1_helpdesk_ticket.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration
            .GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<HelpdeskDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(provider =>
            provider.GetRequiredService<HelpdeskDbContext>());
            
        services.AddScoped<ITicketCodeGenerator, PostgresTicketCodeGenerator>();
        
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        
        return services;
    }
}
