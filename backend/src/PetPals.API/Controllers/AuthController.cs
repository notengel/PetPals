using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPals.Application.Abstractions.Auth;
using PetPals.Application.DTOs.Auth;

namespace PetPals.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return result.Succeeded
            ? Ok(result.Response)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result.Succeeded
            ? Ok(result.Response)
            : Unauthorized(new { errors = result.Errors });
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> Me()
    {
        return Ok(new
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Email = User.FindFirstValue(ClaimTypes.Email),
            DisplayName = User.FindFirstValue(ClaimTypes.Name),
            Role = User.FindFirstValue(ClaimTypes.Role)
        });
    }
}
