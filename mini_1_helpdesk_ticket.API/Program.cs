using System.Text.Json.Serialization;
using mini_1_helpdesk_ticket.API.Extensions;
using mini_1_helpdesk_ticket.API.Middleware;
using mini_1_helpdesk_ticket.Application;
using mini_1_helpdesk_ticket.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerServices();
builder.Services.ConfigureRateLimiter();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerAPI();
}

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseRateLimiter();

app.MapControllers();

app.Run();

public partial class Program;
