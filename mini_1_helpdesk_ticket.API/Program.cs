using System.Reflection.Metadata;
using mini_1_helpdesk_ticket.API.Extensions;
using mini_1_helpdesk_ticket.API.Middleware;
using mini_1_helpdesk_ticket.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using MailService = mini_1_helpdesk_ticket.Service.MailService;
using JwtService = mini_1_helpdesk_ticket.Service.JwtService;
using LabelService = mini_1_helpdesk_ticket.Service.Labels;
using TicketService =  mini_1_helpdesk_ticket.Service.Tickets;

    var builder = WebApplication.CreateBuilder(args);
    
    builder.Services.AddControllers();
    // Add services to the container.
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddDbContext<HelpdeskDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
        .UseSnakeCaseNamingConvention()
    );

    builder.Services.ConfigureRateLimiter();
    builder.Services.AddJwtServices(builder.Configuration);
    builder.Services.AddSwaggerServices();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddScoped<MailService.IService, MailService.Service>();
    builder.Services.AddScoped<JwtService.IService, JwtService.Service>();
    builder.Services.AddScoped<LabelService.IService, LabelService.Service>();
    builder.Services.AddScoped<TicketService.IService, TicketService.Service>();

    
    builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();
    builder.Services.AddScoped<TicketCodeGenerator>();
    
    //builder.Services.AddValidatorsFromAssembly(AssemblyReference.Assembly);

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

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
