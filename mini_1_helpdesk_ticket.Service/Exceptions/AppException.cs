namespace mini_1_helpdesk_ticket.Service.Exceptions;

public abstract class AppException: Exception
{
    public string Title { get; set; } 
    public int StatusCode { get; set; }
    public string MessageCode { get; set; }
    
    protected AppException(
        string title, int statusCode, string messageCode,  string detail): base(detail)
    {
        Title = title;
        StatusCode = statusCode;
        MessageCode = messageCode;
    }

}