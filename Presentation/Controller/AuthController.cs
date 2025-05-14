using Microsoft.AspNetCore.Http;
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
    public async Task<IActionResult> SignUp([FromBody] SignUpForm form, string verificationCode)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.VerifyCodeAndCreateAccountAsync(form, verificationCode);
        return result.Succeeded ? Ok(result) : Problem(result.Message);
    }

    [HttpPost("sendrequest")]
    public async Task<IActionResult> SendEmailRequest(string email)
    {
        if (!string.IsNullOrWhiteSpace(email))
            return BadRequest(email);

        var result = await _authService.VerificationCodeRequestAsync(email);
        return result.Succeeded ? Ok() : BadRequest(result.Message);
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInForm form)
    {
        if(!ModelState.IsValid)
            return Unauthorized("Invalid credentials.");

        var result = await _authService.SignInAsync(form);
        return result.Succeeded ? Ok(result) : Unauthorized(result.Message);
    }
}
