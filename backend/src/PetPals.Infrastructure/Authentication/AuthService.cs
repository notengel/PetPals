using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PetPals.Application.Abstractions.Auth;
using PetPals.Application.DTOs.Auth;
using PetPals.Domain.Enums;
using PetPals.Infrastructure.Configuration;
using PetPals.Infrastructure.Identity;

namespace PetPals.Infrastructure.Authentication;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            return AuthResult.Failure("Role must be User, Clinic or Shelter.");
        }

        var normalizedEmail = request.Email.Trim();
        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return AuthResult.Failure("An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? normalizedEmail[..normalizedEmail.IndexOf('@')]
                : request.DisplayName.Trim(),
            EmailConfirmed = false
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return AuthResult.Failure(createResult.Errors.Select(error => error.Description).ToArray());
        }

        var roleName = role.ToString();
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                return AuthResult.Failure(roleResult.Errors.Select(error => error.Description).ToArray());
            }
        }

        var assignmentResult = await userManager.AddToRoleAsync(user, roleName);
        if (!assignmentResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return AuthResult.Failure(assignmentResult.Errors.Select(error => error.Description).ToArray());
        }

        return AuthResult.Success(CreateAuthResponse(user, normalizedEmail, roleName));
    }

    public async Task<AuthResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return AuthResult.Failure("Invalid email or password.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.SingleOrDefault();
        if (role is null)
        {
            return AuthResult.Failure("The account has no assigned role.");
        }

        return AuthResult.Success(CreateAuthResponse(user, normalizedEmail, role));
    }

    public async Task<(bool Succeeded, string? Error)> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default)
    {
        var email = newEmail.Trim();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return (false, "Account not found.");
        if (await userManager.FindByEmailAsync(email) is not null) return (false, "An account with this email already exists.");
        var emailResult = await userManager.SetEmailAsync(user, email);
        if (!emailResult.Succeeded) return (false, string.Join(", ", emailResult.Errors.Select(e => e.Description)));
        var nameResult = await userManager.SetUserNameAsync(user, email);
        return nameResult.Succeeded ? (true, null) : (false, string.Join(", ", nameResult.Errors.Select(e => e.Description)));
    }

    public async Task<(bool Succeeded, string? Error)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return (false, "Account not found.");
        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded ? (true, null) : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    private AuthResponse CreateAuthResponse(ApplicationUser user, string email, string role)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc,
            user.Id,
            email,
            role);
    }
}
