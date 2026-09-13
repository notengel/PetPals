namespace PetPals.Application.DTOs.Auth;

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Email,
    string Role);

public sealed record AuthResult(
    bool Succeeded,
    AuthResponse? Response = null,
    IReadOnlyCollection<string>? Errors = null)
{
    public static AuthResult Success(AuthResponse response) => new(true, response);

    public static AuthResult Failure(params string[] errors) => new(false, null, errors);
}
