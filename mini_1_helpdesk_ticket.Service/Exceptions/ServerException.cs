namespace mini_1_helpdesk_ticket.Service.Exceptions;

public class ServerException : AppException
{
    public ServerException(string detail)
        : base(title: "Internal Server Error", statusCode: 500, messageCode: "INTERNAL_SERVER_ERROR", detail: detail) { }
}