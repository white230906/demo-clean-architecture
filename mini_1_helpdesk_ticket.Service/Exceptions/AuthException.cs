namespace mini_1_helpdesk_ticket.Service.Exceptions;

public class MissingAccessTokenException : AppException
{
    public MissingAccessTokenException()
        : base("Unauthorized", 401, "MISSING_ACCESS_TOKEN", 
            "Access token is missing.") { }
}

public class ExpiredAccessTokenException : AppException
{
    public ExpiredAccessTokenException()
        : base("Unauthorized", 401, "EXPIRED_ACCESS_TOKEN", 
            "Access token has expired.") { }
}

public class ExpiredRefreshTokenException : AppException
{
    public ExpiredRefreshTokenException()
        : base("Unauthorized",401,  "EXPIRED_REFRESH_TOKEN", 
            "Refresh token has expired. Please login again.") { }
}