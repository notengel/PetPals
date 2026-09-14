using System.ComponentModel.DataAnnotations;
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
    IReadOnlyList<VaccinationRecordDto> Vaccinations,
    IReadOnlyList<AdoptablePetPhotoDto> Photos);

public sealed record AdoptablePetPhotoDto(Guid Id, Guid AdoptablePetId, string ImageUrl, DateTime CreatedAtUtc);

public sealed record SetAdoptablePetPrimaryPhotoRequest(Guid PhotoId);

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

public sealed record ShelterDto(
    Guid Id,
    string Name,
    string? Description,
    string? Phone,
    string? Address,
    string? LogoUrl,
    string? BannerUrl,
    double Latitude,
    double Longitude,
    bool IsVerified);

public sealed class UpsertShelterRequest
{
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;
    [MaxLength(1000)]
    public string? Description { get; init; }
    [MaxLength(30)]
    public string? Phone { get; init; }
    [MaxLength(300)]
    public string? Address { get; init; }
    [MaxLength(500)]
    public string? LogoUrl { get; init; }
    [MaxLength(500)]
    public string? BannerUrl { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

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
