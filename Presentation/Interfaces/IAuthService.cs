using Presentation.Models;

namespace Presentation.Interfaces
{
    public interface IAuthService
    {
        Task<SignInResult> SignInAsync(SignInForm formData);
        //Task<SignUpResult> SignUpAsync(SignUpForm formData);
        Task<SignUpResult> VerificationCodeRequestAsync(string email);
        Task<SignUpResult> VerifyCodeAndCreateAccountAsync(SignInForm formData, string verificationCode);
    }
}