namespace Presentation.Models;

public class SignUpResult
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public string? UserId { get; set; }
}
