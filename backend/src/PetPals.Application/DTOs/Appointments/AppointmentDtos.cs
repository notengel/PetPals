using PetPals.Domain.Enums;

namespace PetPals.Application.DTOs.Appointments;

public sealed record ClinicServiceDto(
    Guid Id,
    Guid ClinicId,
    string Name,
    string? Description,
    decimal? Price,
    int? DurationMinutes,
    bool IsActive);

public sealed record ClinicScheduleDto(
    Guid Id,
    Guid ClinicId,
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt);

public sealed record AppointmentPetDto(Guid Id, string Name, string Species);

public sealed record AppointmentDto(
    Guid Id,
    Guid UserId,
    Guid ClinicId,
    Guid ClinicServiceId,
    string ServiceName,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    AppointmentStatus Status,
    string? UserNotes,
    string? ClinicNotes,
    IReadOnlyList<AppointmentPetDto> Pets);

public sealed record CreateClinicServiceRequest(
    string Name,
    string? Description,
    decimal? Price,
    int DurationMinutes);

public sealed record CreateClinicScheduleRequest(
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt);

public sealed record CreateAppointmentRequest(
    Guid ClinicId,
    Guid ClinicServiceId,
    IReadOnlyList<Guid> PetIds,
    DateTime StartsAtUtc,
    string? UserNotes);

public sealed record UpdateAppointmentStatusRequest(
    AppointmentStatus Status,
    string? ClinicNotes);

public sealed record AppointmentResult<T>(bool Succeeded, T? Data, string? Error)
{
    public static AppointmentResult<T> Success(T data) => new(true, data, null);
    public static AppointmentResult<T> Failure(string error) => new(false, default, error);
}
