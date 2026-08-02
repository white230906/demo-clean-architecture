using System.Security.Claims;

namespace mini_1_helpdesk_ticket.Service.JwtService;

public interface IService
{
    public string GenerateAccessToken(IEnumerable<Claim> claims);
    public string GenerateRefreshToken();
    public string GenerateEmailVerificationToken(IEnumerable<Claim> claims, double expiration);
    public ClaimsPrincipal ValidateToken(string token);
}