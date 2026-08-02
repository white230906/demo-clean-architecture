using System.ComponentModel.DataAnnotations;

namespace mini_1_helpdesk_ticket.Service.Labels;

public class Request
{
    public class CreateLabelRequest
    {
        [property: Required, MaxLength(100)] 
        public string Name { get; set; } = null!;

        // [property: Required, MaxLength(100), RegularExpression(
        //                "^[a-z0-9]+(-[a-z0-9]+)*$",
        //                ErrorMessage = "Slug chỉ gồm chữ thường, số và gạch ngang.")]
        // public string Slug { get; set; } = null!;

        [property: Required, RegularExpression(
                       "^#[0-9A-Fa-f]{6}$",
                       ErrorMessage = "Color phải có dạng #RRGGBB.")]
        public string Color { get; set; } = null!;
        
    }
}