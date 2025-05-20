using Microsoft.Extensions.Options;
using Presentation.Interfaces;
using Presentation.Models;
using System.Text.Json;

namespace Presentation.Services;

public class AuthService : IAuthService
{
    private readonly AccountGrpcService.AccountGrpcServiceClient _accountClient;
    private readonly AuthServiceBusHandler _serviceBus;
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;

    public AuthService(AccountGrpcService.AccountGrpcServiceClient accountClient, AuthServiceBusHandler serviceBus, HttpClient httpClient, IConfiguration configuration, IOptions<ApiSettings> apiSettings)
    {
        _accountClient = accountClient;
        _serviceBus = serviceBus;
        _httpClient = httpClient;
        _apiSettings = apiSettings.Value;
    }

    public async Task<SignUpResult> VerificationCodeRequestAsync(string email)
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

    public async Task<SignUpResult> VerifyCodeAsync(VerifyForm formData)
    {
        if (formData == null || string.IsNullOrWhiteSpace(formData.Email) || string.IsNullOrWhiteSpace(formData.Code))
        {
            return new SignUpResult { Succeeded = false, Message = "Email and verification code are required." };
        }

        try
        {
            var payload = new
            {
                email = formData.Email,
                code = formData.Code,
            };

            var response = await _httpClient.PostAsJsonAsync($"https://verificationserviceprovider.azurewebsites.net/api/ValidateVerificationCode?code={_apiSettings.verificationCodeKey}", payload);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                var message = "";

                try
                {
                    var errorObj = JsonSerializer.Deserialize<Dictionary<string, string>>(errorContent);
                    if (errorObj != null && errorObj.ContainsKey("message"))
                    {
                        message = errorObj["message"];
                    }
                } catch
                {
                    message = errorContent;
                }

                return new SignUpResult { Succeeded = false, Message = message };
            }

            return new SignUpResult { Succeeded = true, Message = "The account is verified" };

        }
        catch (HttpRequestException httpEx)
        {
            return new SignUpResult
            {
                Succeeded = false,
                Message = $"Network error: {httpEx.Message}"
            };
        } 
        catch (Exception ex)
        {
            return new SignUpResult { Succeeded = false, Message = ex.Message };
        }
    }

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

            var accountReplay = await _accountClient.GetAccountAsync(new GetAccountRequest { UserId = reply.UserId });
            if (!accountReplay.Succeeded)
            {
                return new SignInResult { Succeeded = false, Message = "Failed to retrieve account info." };
            }

            // Generate Token

            return new SignInResult { Succeeded = reply.Succeeded, Message = reply.Message, UserId = reply.UserId, RoleName = accountReplay.Account.RoleName };
        }
        catch (Exception ex)
        {
            return new SignInResult { Succeeded = false, Message = ex.Message };
        }
    }
}
