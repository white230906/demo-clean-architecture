namespace mini_1_helpdesk_ticket.Domain.Common;

public sealed class DomainException : Exception
{
    public string Code { get; }
    public object? Details { get; }

    public DomainException(
        string code,
        string message,
        object? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }

}
