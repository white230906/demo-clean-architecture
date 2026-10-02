using mini_1_helpdesk_ticket.API.Models;
using mini_1_helpdesk_ticket.Application.Common.Exceptions;
using mini_1_helpdesk_ticket.Domain.Common;

namespace mini_1_helpdesk_ticket.API.Middleware;

public sealed class GlobalExceptionHandlerMiddleware : IMiddleware
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

    public async Task InvokeAsync(
        HttpContext context,
        RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var error = MapException(exception);

            if (error.Status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception while processing {Path}",
                    context.Request.Path);
            }
            else
            {
                _logger.LogWarning(
                    "Request {Path} failed with {Code}: {Message}",
                    context.Request.Path,
                    error.Code,
                    exception.Message);
            }

            context.Response.Clear();
            context.Response.StatusCode = error.Status;
            context.Response.ContentType = "application/json";

            var response = ApiResponseFactory.Error(
                error.Title,
                error.Status,
                error.Status >= StatusCodes.Status500InternalServerError &&
                !_environment.IsDevelopment()
                    ? "An unexpected error occurred."
                    : exception.Message,
                error.Code,
                error.Details,
                context.TraceIdentifier);

            await context.Response.WriteAsJsonAsync(
                response,
                context.RequestAborted);
        }
    }

    private static ErrorDescriptor MapException(Exception exception)
    {
        return exception switch
        {
            AppException appException => MapApplicationException(appException),

            DomainException domainException => new ErrorDescriptor(
                domainException.Code == "TICKET_CLOSED"
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest,
                domainException.Code == "TICKET_CLOSED"
                    ? "Conflict"
                    : "Bad Request",
                domainException.Code,
                domainException.Details),

            _ => new ErrorDescriptor(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "INTERNAL_SERVER_ERROR",
                null)
        };
    }

    private static ErrorDescriptor MapApplicationException(
        AppException exception)
    {
        var (status, title) = exception.Type switch
        {
            ErrorType.Validation =>
                (StatusCodes.Status400BadRequest, "Bad Request"),

            ErrorType.NotFound =>
                (StatusCodes.Status404NotFound, "Not Found"),

            ErrorType.Conflict =>
                (StatusCodes.Status409Conflict, "Conflict"),

            ErrorType.Unauthorized =>
                (StatusCodes.Status401Unauthorized, "Unauthorized"),

            ErrorType.Forbidden =>
                (StatusCodes.Status403Forbidden, "Forbidden"),

            _ =>
                (StatusCodes.Status500InternalServerError,
                    "Internal Server Error")
        };

        return new ErrorDescriptor(
            status,
            title,
            exception.Code,
            exception.Details);
    }

    private sealed record ErrorDescriptor(
        int Status,
        string Title,
        string Code,
        object? Details);
}
