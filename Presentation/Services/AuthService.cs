using Presentation.Interfaces;
using Presentation.Models;

namespace Presentation.Services;

public class AuthService(AccountGrpcService.AccountGrpcServiceClient accountClient) : IAuthService
{
    private readonly AccountGrpcService.AccountGrpcServiceClient _accountClient = accountClient;

    public async Task<SignUpResult> SignUpAsync(SignUpForm formData)
    {
        try
        {

            await _serviceBus.PublishAsync(email);

            return new SignUpResult { Succeeded = true, Message = "Verification code sent to email." };

        }
        catch (Exception ex)
        {
            return new SignUpResult { Succeeded = false, Message = ex.Message };
        }
    }

    public async Task<SignUpResult> VerifyCodeAndCreateAccountAsync(SignUpForm formData, string verificationCode)
    {
        try
        {
            var payload = new
            {
                Email = formData.Email,
                Code = verificationCode
            };

            var response = await _httpClient.PostAsJsonAsync("https://verificationserviceprovider.azurewebsites.net/api/ValidateVerificationCode?code=", payload);

            if (!response.IsSuccessStatusCode)
            {
                return new SignUpResult
                {
                    Succeeded = false,
                    Message = "Verification failed"
                };
            }
            var request = new CreateAccountRequest
            {
                Email = formData.Email,
                Password = formData.Password,
            };

            var reply = await _accountClient.CreateAccountAsync(request);
            return reply.Succeeded
                ? new SignUpResult { Succeeded = reply.Succeeded, Message = reply.Message, UserId = reply.UserId } // Kolla om du ska sätta in role här
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

            return new SignInResult { Succeeded = reply.Succeeded, Message = reply.Message, UserId = reply.UserId };
        }
        catch (Exception ex)
        {
            return new SignInResult { Succeeded = false, Message = ex.Message };
        }
    }
}
