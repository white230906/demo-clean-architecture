using System.Security.Claims;
using System.Text;
using mini_1_helpdesk_ticket.Service.Exceptions;
using mini_1_helpdesk_ticket.Service.JwtService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace mini_1_helpdesk_ticket.API.Extensions;

public static class JwtExtensions
{
    public const string AdminPolicy = "AdminPolicy";
    public const string StaffPolicy = "StaffPolicy";
    public const string UserPolicy = "UserPolicy";
    public const string StaffOrAdminPolicy = "StaffOrAdminPolicy";
    
    public static void AddJwtServices(this IServiceCollection services, IConfiguration configuration)
    {
        JwtOption jwtOption = new JwtOption();
        configuration.GetSection(nameof(JwtOption)).Bind(jwtOption);
        var key = Encoding.UTF8.GetBytes(jwtOption.AccessTokenKey);
    
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOption.Issuer,
                    ValidAudience = jwtOption.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role,
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrWhiteSpace(context.Token)
                            && context.Request.Cookies.TryGetValue(CookieExtensions.AccessTokenCookieName, out var token)
                            && !string.IsNullOrWhiteSpace(token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            throw new ExpiredAccessTokenException();
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, policy =>
                policy.RequireRole("Admin"));
            // [Authorize(Policy = JwtExtensions.AdminPolicy)]
        
            options.AddPolicy(StaffPolicy, policy =>
                policy.RequireRole("Staff"));
            // [Authorize(Policy = JwtExtensions.StaffPolicy)]
        
            options.AddPolicy(UserPolicy, policy =>
                policy.RequireRole("User"));
        
            options.AddPolicy(StaffOrAdminPolicy, policy =>
                policy.RequireRole("Staff", "Admin"));
        
            // [Authorize(Policy = JwtExtensions.StaffOrAdminPolicy)]
        });
    }
}