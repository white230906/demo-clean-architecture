namespace mini_1_helpdesk_ticket.Application.Common.Exceptions;

public class AppException : Exception
{
    public string Code { get; }
    public ErrorType Type { get; }
    public object? Details { get; }

    public AppException(ErrorType type, string code, string message, object? details = null)
        : base(message)
    {
        Type = type;
        Code = code;
        Details = details;
    }
}
