using PetPals.Domain.Enums;

namespace PetPals.Application.DTOs.Adoptions;

public sealed record AdoptablePetDto(
    Guid Id,
    Guid ShelterId,
    string ShelterName,
    string Name,
    string Species,
    string? Breed,
    string? Sex,
    string? ApproximateAge,
    string? Description,
    AdoptablePetStatus Status,
    string? PrimaryImageUrl,
    IReadOnlyList<VaccinationRecordDto> Vaccinations);

public sealed record VaccinationRecordDto(
    Guid Id,
    Guid AdoptablePetId,
    string VaccineName,
    DateOnly AppliedOn,
    DateOnly? NextDueOn,
    string? Notes);

public sealed record AdoptionRequestDto(
    Guid Id,
    Guid AdoptablePetId,
    string PetName,
    Guid ApplicantUserId,
    AdoptionRequestStatus Status,
    string? ApplicantMessage,
    string? ShelterComment,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc);

public sealed record UpsertShelterRequest(
    string Name,
    string? Description,
    string? Phone,
    string? Address,
    double Latitude,
    double Longitude);

public sealed record CreateAdoptablePetRequest(
    string Name,
    string Species,
    string? Breed,
    string? Sex,
    string? ApproximateAge,
    string? Description,
    string? PrimaryImageUrl);

public sealed record CreateVaccinationRecordRequest(
    string VaccineName,
    DateOnly AppliedOn,
    DateOnly? NextDueOn,
    string? Notes);

public sealed record CreateAdoptionRequest(string? ApplicantMessage);

public sealed record UpdateAdoptionRequest(
    AdoptionRequestStatus Status,
    string? ShelterComment);

public sealed record AdoptionResult<T>(bool Succeeded, T? Data, string? Error)
{
    public static AdoptionResult<T> Success(T data) => new(true, data, null);
    public static AdoptionResult<T> Failure(string error) => new(false, default, error);
}
