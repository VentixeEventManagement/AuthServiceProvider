using Presentation.Models;

namespace Presentation.Interfaces;

public interface IAuthService
{
    Task<SignInResult> SignInAsync(SignInForm formData);
    Task<SignUpResult> VerificationCodeRequestAsync(string email);
    Task<SignUpResult> VerifyCodeAndCreateAccountAsync(SignUpForm formData, string verificationCode);
    Task<SignUpResult> VerifyCodeAsync(VerifyForm formData);
}