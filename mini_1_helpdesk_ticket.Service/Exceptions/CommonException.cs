namespace mini_1_helpdesk_ticket.Service.Exceptions;

public class CommonException
{
    public sealed class BadRequestException : AppException
    {
        public BadRequestException(string messageCode, string detail)
            : base("Bad Request", 400, messageCode, detail) { }
    }

    public sealed class NotFoundException : AppException
    {
        public NotFoundException(string messageCode, string detail)
            : base("Not Found", 404, messageCode, detail) { }
    }

    public sealed class ConflictException : AppException
    {
        public ConflictException(string messageCode, string detail)
            : base("Conflict", 409, messageCode, detail) { }
    }
}