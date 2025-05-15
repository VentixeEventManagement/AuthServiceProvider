namespace Presentation.Models;

public class VerifyResult : SignUpResult
{
    public string VerificationCode { get; set; } = null!;
}
