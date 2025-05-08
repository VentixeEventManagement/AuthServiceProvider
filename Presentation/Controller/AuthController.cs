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
    public async Task<IActionResult> SignUp([FromBody] SignUpForm form)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.SignUpAsync(form);
        return result.Succeeded ? Ok(result) : Problem(result.Message);
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInForm form)
    {
        if(!ModelState.IsValid)
            return Unauthorized("Invalid credentials.");

        var result = await _authService.SignInAsync(form);
        return result.Succeeded? Ok(result) : Unauthorized(result.Message);
    }
}
