using Microsoft.AspNetCore.Mvc;
using Presentation.Documentation;
using Presentation.Interfaces;
using Presentation.Models;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Presentation.Controller;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("signup")]
    [Consumes("application/json")]
    [SwaggerOperation(Summary = "Register a new user.")]
    [SwaggerResponse(StatusCodes.Status200OK, "User registration succeeded.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid registration data.")]
    [SwaggerRequestExample(typeof(SignUpForm), typeof(SignUpForm_Example))]  // Om du har en exempelklass
    public async Task<IActionResult> SignUp([FromBody] SignUpForm form)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.SignUpAsync(form);
        return result.Succeeded ? Ok(result) : Problem(result.Message);
    }

    [HttpPost("sendrequest")]
    [SwaggerOperation(Summary = "Request a verification code by email.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Verification code sent.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid email.")]
    public async Task<IActionResult> SendEmailRequest([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required.");

        var result = await _authService.VerificationCodeRequestAsync(email);
        return result.Succeeded ? Ok(result.Message) : BadRequest(result.Message);
    }

    [HttpPost("verify")]
    [Consumes("application/json")]
    [SwaggerOperation(Summary = "Verify a given code.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Verification succeeded.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid verification data.")]
    [SwaggerRequestExample(typeof(VerifyForm), typeof(VerifyForm_Example))] // Om du har en exempelklass
    public async Task<IActionResult> VerifyCodeAsync([FromBody] VerifyForm formData)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.VerifyCodeAsync(formData);
        return result.Succeeded ? Ok(result.Message) : Problem(result.Message);
    }

    [HttpPost("signin")]
    [Consumes("application/json")]
    [SwaggerOperation(Summary = "Sign in a user.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Sign-in succeeded.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid credentials.")]
    [SwaggerRequestExample(typeof(SignInForm), typeof(SignInForm_Example))] // Om du har en exempelklass
    public async Task<IActionResult> SignIn([FromBody] SignInForm form)
    {
        if (!ModelState.IsValid)
            return Unauthorized("Invalid credentials.");

        var result = await _authService.SignInAsync(form);
        return result.Succeeded ? Ok(result) : Unauthorized(result.Message);
    }

    [HttpGet("getaccounts")]
    [SwaggerOperation(Summary = "Get all accounts.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Accounts retrieved successfully.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "No accounts found.")]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _authService.GetAllAccountsAsync();
        if (accounts == null)
            return BadRequest("No accounts found.");

        return Ok(accounts);
    }

    [HttpGet("getaccount")]
    [SwaggerOperation(Summary = "Get account info by user ID.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Account info retrieved successfully.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid user ID or account not found.")]
    public async Task<IActionResult> GetAccountInfo([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("Invalid user id.");

        var account = await _authService.GetAccountInfoAsync(userId);
        if (account == null)
            return BadRequest("Account is null.");

        return Ok(account);
    }

    [HttpPut("changerole")]
    [SwaggerOperation(Summary = "Change the role of a user.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Role updated successfully.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "UserId and role are required.")]
    public async Task<IActionResult> ChangeRole([FromQuery] string userId, [FromQuery] string role)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
            return BadRequest("UserId and role are required.");

        var result = await _authService.UpdateRoleAsync(userId, role);

        return result.Succeeded ? Ok(result) : Problem(result.Message);
    }
}
