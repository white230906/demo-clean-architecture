using System.ComponentModel.DataAnnotations;

namespace mini_1_helpdesk_ticket.Service.JwtService;

public class JwtOption
{
    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    public string AccessTokenKey { get; set; } = string.Empty;

    [Required]
    public int AccessTokenExpireMin { get; set; }

    [Required]
    public string RefreshTokenKey { get; set; } = string.Empty;

    [Required]
    public int RefreshTokenExpireMin { get; set; }
}
