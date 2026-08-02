using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace mini_1_helpdesk_ticket.Service.JwtService;

public class Service: IService
{
    private readonly JwtOption _jwtOption = new();

    public Service(IConfiguration configuration)
    {
        configuration.GetSection(nameof(JwtOption)).Bind(_jwtOption);
    }
    
    public string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOption.AccessTokenKey));
  
        var signingCredentials = new SigningCredentials(
            secretKey, 
            SecurityAlgorithms.HmacSha256);

        var tokenOptions = new JwtSecurityToken( 
            issuer: _jwtOption.Issuer, 
            audience: _jwtOption.Audience, 
            claims: claims, 
            expires: DateTime.Now.AddMinutes(_jwtOption.AccessTokenExpireMin),
            signingCredentials: signingCredentials
        );
        
        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
     
        return tokenString;
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public string GenerateEmailVerificationToken(IEnumerable<Claim> claims, double expiration)
    {
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOption.AccessTokenKey));
  
        var signingCredentials = new SigningCredentials(
            secretKey, 
            SecurityAlgorithms.HmacSha256);

        var tokenOptions = new JwtSecurityToken( 
            issuer: _jwtOption.Issuer, 
            audience: _jwtOption.Audience, 
            claims: claims, 
            expires: DateTime.Now.AddHours(expiration),
            signingCredentials: signingCredentials
        );
        
        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
     
        return tokenString;
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtOption.AccessTokenKey); // Sử dụng _jwtOption 

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtOption.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOption.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            Console.WriteLine($"Token validation failed: {ex.Message}");
            return null!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error during token validation: {ex.Message}");
            return null!;
        }
    }
}