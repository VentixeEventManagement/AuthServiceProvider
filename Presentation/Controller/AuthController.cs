using Microsoft.AspNetCore.Mvc;
using Presentation.Interfaces;
using Presentation.Models;

namespace Presentation.Controller;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpForm form)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.SignUpAsync(form);
        return result.Succeeded ? Ok(result) : Problem(result.Message);
    }

    [HttpPost("sendrequest")]
    public async Task<IActionResult> SendEmailRequest(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(email);

        var result = await _authService.VerificationCodeRequestAsync(email);
        return result.Succeeded ? Ok(result.Message) : BadRequest(result.Message);
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyCodeAsync(VerifyForm formData)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.VerifyCodeAsync(formData);
        return result.Succeeded ? Ok(result.Message) : Problem(result.Message);
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInForm form)
    {
        if(!ModelState.IsValid)
            return Unauthorized("Invalid credentials.");

        var result = await _authService.SignInAsync(form);
        return result.Succeeded ? Ok(result) : Unauthorized(result.Message);
    }

    [HttpGet("getaccount")]
    public async Task<IActionResult> GetAccountInfo([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("Invalid user id.");

        var account = await _authService.GetAccountInfoAsync(userId);
        if (account == null)
            return BadRequest("Account is null.");

        return Ok(account);
    }
}
