using Microsoft.Extensions.DependencyInjection;

namespace mini_1_helpdesk_ticket.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<Labels.IService, Labels.Service>();
        services.AddScoped<Tickets.IService, Tickets.Service>();
        
        return services;
    }
}