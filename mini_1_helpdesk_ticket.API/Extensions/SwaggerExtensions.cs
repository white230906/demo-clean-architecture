using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace mini_1_helpdesk_ticket.API.Extensions;

public static class SwaggerExtensions
{
    public static void AddSwaggerServices(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo() { Title = "mini_1_helpdesk_ticket APIs", Version = "v1" });
        });
    }
    
    public static void UseSwaggerAPI(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            // foreach (var version in app.DescribeApiVersions().Select(version => version.GroupName))
            //     options.SwaggerEndpoint($"/swagger/{version}/swagger.json", version);

            options.DisplayRequestDuration();   //hien thi thoi gian thuc hien cuq req nay
            options.EnableTryItOutByDefault(); //
            options.DocExpansion(DocExpansion.List); //
        });

        app.MapGet("/", () => Results.Redirect("/swagger/index.html"))
            .WithTags(string.Empty);
    }
}
