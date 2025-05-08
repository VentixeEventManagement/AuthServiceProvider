﻿namespace Presentation.Models;

public class SignInResult
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public string? UserId { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}
