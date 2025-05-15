using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public class SignUpForm
{
    [Required]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = null!;

    [Required]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", ErrorMessage = "Invalid password format")]
    public string Password { get; set; } = null!;

    [Required]
    public bool verified { get; set; }
}
