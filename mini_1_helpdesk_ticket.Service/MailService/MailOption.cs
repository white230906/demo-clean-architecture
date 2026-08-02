using System.ComponentModel.DataAnnotations;

namespace mini_1_helpdesk_ticket.Service.MailService;

public class MailOptions
{
    [Required]public string Mail { get; set; }
    [Required]public string DisplayName { get; set; }
    [Required]public string Password { get; set; }
    [Required]public string Host { get; set; }
    [Required]public int Port { get; set; }
}