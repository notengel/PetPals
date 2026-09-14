using PetPals.Application.DTOs.Auth;

namespace PetPals.Application.Abstractions.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}
