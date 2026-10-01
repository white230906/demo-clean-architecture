using System.Text.Json.Serialization;
using mini_1_helpdesk_ticket.API.Extensions;
using mini_1_helpdesk_ticket.API.Middleware;
using mini_1_helpdesk_ticket.Repo;
using Microsoft.EntityFrameworkCore;
using LabelService = mini_1_helpdesk_ticket.Service.Labels;
using TicketService =  mini_1_helpdesk_ticket.Service.Tickets;

    var builder = WebApplication.CreateBuilder(args);
    
    builder.Services.AddControllers()
        .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    // Add services to the container.
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddDbContext<HelpdeskDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
        .UseSnakeCaseNamingConvention()
    );

    builder.Services.ConfigureRateLimiter();
    builder.Services.AddSwaggerServices();

    builder.Services.AddScoped<LabelService.IService, LabelService.Service>();
    builder.Services.AddScoped<TicketService.IService, TicketService.Service>();

    
    builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();
    builder.Services.AddScoped<TicketCodeGenerator>();
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

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerAPI();
    }

    app.UseCors("AllowFrontend");

    app.UseRateLimiter();

    app.MapControllers();

    app.Run();
