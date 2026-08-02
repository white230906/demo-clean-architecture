
using mini_1_helpdesk_ticket.Service.Exceptions;
using mini_1_helpdesk_ticket.Service.Models;

namespace mini_1_helpdesk_ticket.API.Middleware;

public class GlobalExceptionHandlerMiddleware : IMiddleware
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        IHostEnvironment environment,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred while processing request {Path}", 
                context.Request.Path);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("The response has already started, the global exception middleware will not write an error response");
                throw;
            }
            
            
            //context.Response.Clear();
            context.Response.ContentType = "application/json";
            
            AppException appEx = ex switch
            {
                AppException alreadyAppEx => alreadyAppEx,
                _ => new ServerException("An unexpected system error occurred.") 
            };
            
            context.Response.StatusCode = appEx.StatusCode;
            
            var response = ApiResponseFactory.Error(
                title: appEx.Title,
                status: appEx.StatusCode,
                detail: appEx.StatusCode >= 500 ? "An unexpected error occurred." : ex.Message,
                messageCode: appEx.MessageCode,
                errors: _environment.IsDevelopment() ? new { detail = ex.Message } : null, 
                traceId: context.TraceIdentifier
            );
            
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}