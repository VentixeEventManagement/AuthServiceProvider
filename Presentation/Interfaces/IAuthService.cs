using Presentation.Models;

namespace Presentation.Interfaces;

public interface IAuthService
{
    Task<SignInResult> SignInAsync(SignInForm formData);
    Task<SignUpResult> VerificationCodeRequestAsync(string email);
    Task<SignUpResult> SignUpAsync(SignUpForm formData);
    Task<SignUpResult> VerifyCodeAsync(VerifyForm formData);
}