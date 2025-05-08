using Presentation.Models;

namespace Presentation.Services;

public class AuthService(AccountGrpcService.AccountGrpcServiceClient accountClient)
{
    private readonly AccountGrpcService.AccountGrpcServiceClient _accountClient = accountClient;

    public async Task<SignUpResult> SignUpAsync(SignUpForm formData)
    {
        try
        {
            var request = new CreateAccountRequest
            {
                Email = formData.Email,
                Password = formData.Password,
            };

            var reply = await _accountClient.CreateAccountAsync(request);
            return reply.Succeeded
                ? new SignUpResult { Succeeded = reply.Succeeded, Message = reply.Message, UserId = reply.UserId }
                : new SignUpResult { Succeeded = reply.Succeeded, Message = reply.Message };
        }
        catch (Exception ex)
        {
            return new SignUpResult { Succeeded = false, Message = ex.Message };
        }
    }

    public async Task<SignInResult> SignInAsync(SignInForm formData)
    {
        try
        {
            var request = new ValidateCredentialsRequest
            {
                Email = formData.Email,
                Password = formData.Password,
            };

            var reply = await _accountClient.ValidateCredentialsAsync(request);
            if (!reply.Succeeded)
            {
                return new SignInResult { Succeeded = reply.Succeeded, Message = reply.Message };
            }

            // Generate Token

            return new SignInResult { Succeeded = reply.Succeeded, Message = reply.Message };
        }
        catch (Exception ex)
        {
            return new SignInResult { Succeeded = false, Message = ex.Message };
        }
    }
}
