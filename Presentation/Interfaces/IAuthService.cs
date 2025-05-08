using Presentation.Models;

namespace Presentation.Interfaces
{
    public interface IAuthService
    {
        Task<SignInResult> SignInAsync(SignInForm formData);
        Task<SignUpResult> SignUpAsync(SignUpForm formData);
    }
}