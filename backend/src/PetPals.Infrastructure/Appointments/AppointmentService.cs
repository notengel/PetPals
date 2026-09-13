using Microsoft.EntityFrameworkCore;
using PetPals.Application.Abstractions.Appointments;
using PetPals.Application.DTOs.Appointments;
using PetPals.Domain.Entities;
using PetPals.Domain.Enums;
using PetPals.Infrastructure.Persistence;

namespace PetPals.Infrastructure.Appointments;

public sealed class AppointmentService(ApplicationDbContext db) : IAppointmentService
{
    public async Task<IReadOnlyList<ClinicServiceDto>> GetServicesAsync(Guid clinicId, CancellationToken cancellationToken = default) =>
        await db.ClinicServices.AsNoTracking()
            .Where(service => service.ClinicId == clinicId && service.IsActive)
            .OrderBy(service => service.Name)
            .Select(service => new ClinicServiceDto(service.Id, service.ClinicId, service.Name,
                service.Description, service.Price, service.DurationMinutes, service.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClinicScheduleDto>> GetSchedulesAsync(Guid clinicId, CancellationToken cancellationToken = default) =>
        await db.ClinicSchedules.AsNoTracking()
            .Where(schedule => schedule.ClinicId == clinicId)
            .OrderBy(schedule => schedule.DayOfWeek)
            .ThenBy(schedule => schedule.OpensAt)
            .Select(schedule => new ClinicScheduleDto(schedule.Id, schedule.ClinicId, schedule.DayOfWeek,
                schedule.OpensAt, schedule.ClosesAt))
            .ToListAsync(cancellationToken);

    public async Task<ClinicServiceDto?> CreateServiceAsync(Guid userId, CreateClinicServiceRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.DurationMinutes <= 0)
        {
            return null;
        }

        var clinic = await db.Clinics.SingleOrDefaultAsync(clinic => clinic.OwnerUserId == userId, cancellationToken);
        if (clinic is null)
        {
            return null;
        }

        var service = new ClinicService
        {
            ClinicId = clinic.Id,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            DurationMinutes = request.DurationMinutes
        };
        db.ClinicServices.Add(service);
        await db.SaveChangesAsync(cancellationToken);
        return new ClinicServiceDto(service.Id, service.ClinicId, service.Name, service.Description,
            service.Price, service.DurationMinutes, service.IsActive);
    }

    public async Task<ClinicScheduleDto?> CreateScheduleAsync(Guid userId, CreateClinicScheduleRequest request, CancellationToken cancellationToken = default)
    {
        if (request.OpensAt >= request.ClosesAt)
        {
            return null;
        }

        var clinic = await db.Clinics.SingleOrDefaultAsync(clinic => clinic.OwnerUserId == userId, cancellationToken);
        if (clinic is null || await db.ClinicSchedules.AnyAsync(schedule =>
                schedule.ClinicId == clinic.Id && schedule.DayOfWeek == request.DayOfWeek &&
                schedule.OpensAt < request.ClosesAt && request.OpensAt < schedule.ClosesAt, cancellationToken))
        {
            return null;
        }

        var schedule = new ClinicSchedule
        {
            ClinicId = clinic.Id,
            DayOfWeek = request.DayOfWeek,
            OpensAt = request.OpensAt,
            ClosesAt = request.ClosesAt
        };
        db.ClinicSchedules.Add(schedule);
        await db.SaveChangesAsync(cancellationToken);
        return new ClinicScheduleDto(schedule.Id, schedule.ClinicId, schedule.DayOfWeek,
            schedule.OpensAt, schedule.ClosesAt);
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetMyAppointmentsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await ToDtos(db.Appointments.AsNoTracking().Where(appointment => appointment.UserId == userId), cancellationToken);

    public async Task<IReadOnlyList<AppointmentDto>> GetClinicAppointmentsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var clinicId = await db.Clinics.Where(clinic => clinic.OwnerUserId == userId)
            .Select(clinic => (Guid?)clinic.Id).SingleOrDefaultAsync(cancellationToken);
        return clinicId is null ? [] : await ToDtos(db.Appointments.AsNoTracking().Where(appointment => appointment.ClinicId == clinicId), cancellationToken);
    }

    public async Task<AppointmentResult<AppointmentDto>> CreateAppointmentAsync(Guid userId, CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var petIds = request.PetIds.Distinct().ToArray();
        if (petIds.Length == 0 || request.StartsAtUtc.Kind != DateTimeKind.Utc || request.StartsAtUtc <= DateTime.UtcNow)
        {
            return AppointmentResult<AppointmentDto>.Failure("The appointment time and at least one pet are required.");
        }

        var service = await db.ClinicServices.SingleOrDefaultAsync(current =>
            current.Id == request.ClinicServiceId && current.ClinicId == request.ClinicId && current.IsActive,
            cancellationToken);
        var pets = await db.Pets.Where(pet => petIds.Contains(pet.Id) && pet.OwnerUserId == userId).ToListAsync(cancellationToken);
        if (service?.DurationMinutes is not > 0 || pets.Count != petIds.Length)
        {
            return AppointmentResult<AppointmentDto>.Failure("The service or one of the pets is invalid.");
        }

        var endsAtUtc = request.StartsAtUtc.AddMinutes(service.DurationMinutes.Value);
        var localStart = TimeOnly.FromDateTime(request.StartsAtUtc);
        var localEnd = TimeOnly.FromDateTime(endsAtUtc);
        var inSchedule = await db.ClinicSchedules.AnyAsync(schedule =>
            schedule.ClinicId == request.ClinicId && schedule.DayOfWeek == request.StartsAtUtc.DayOfWeek &&
            schedule.OpensAt <= localStart && schedule.ClosesAt >= localEnd, cancellationToken);
        var overlaps = await db.Appointments.AnyAsync(appointment =>
            appointment.ClinicId == request.ClinicId &&
            appointment.Status != AppointmentStatus.Cancelled &&
            appointment.StartsAtUtc < endsAtUtc && request.StartsAtUtc < appointment.EndsAtUtc, cancellationToken);
        if (!inSchedule || overlaps)
        {
            return AppointmentResult<AppointmentDto>.Failure("The selected time is outside clinic hours or already booked.");
        }

        var appointment = new Appointment
        {
            UserId = userId,
            ClinicId = request.ClinicId,
            ClinicServiceId = request.ClinicServiceId,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = endsAtUtc,
            UserNotes = request.UserNotes?.Trim(),
            Pets = petIds.Select(petId => new AppointmentPet { PetId = petId }).ToList()
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(cancellationToken);
        return AppointmentResult<AppointmentDto>.Success(await ToDto(appointment.Id, cancellationToken));
    }

    public Task<AppointmentResult<AppointmentDto>> CancelAppointmentAsync(Guid userId, Guid appointmentId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(userId, appointmentId, AppointmentStatus.Cancelled, null, false, cancellationToken);

    public Task<AppointmentResult<AppointmentDto>> UpdateStatusAsync(Guid userId, Guid appointmentId, UpdateAppointmentStatusRequest request, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(userId, appointmentId, request.Status, request.ClinicNotes, true, cancellationToken);

    private async Task<AppointmentResult<AppointmentDto>> ChangeStatusAsync(Guid userId, Guid appointmentId, AppointmentStatus status,
        string? clinicNotes, bool clinicOwner, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments.Include(item => item.Pets)
            .SingleOrDefaultAsync(item => item.Id == appointmentId, cancellationToken);
        var ownsAppointment = appointment is not null && (clinicOwner
            ? await db.Clinics.AnyAsync(clinic => clinic.Id == appointment.ClinicId && clinic.OwnerUserId == userId, cancellationToken)
            : appointment.UserId == userId);
        if (appointment is null || !ownsAppointment)
        {
            return AppointmentResult<AppointmentDto>.Failure("Appointment not found.");
        }

        if (!clinicOwner && (status != AppointmentStatus.Cancelled || appointment.StartsAtUtc <= DateTime.UtcNow.AddHours(24)))
        {
            return AppointmentResult<AppointmentDto>.Failure("Appointments can only be cancelled at least 24 hours in advance.");
        }

        if (clinicOwner && status is not (AppointmentStatus.Confirmed or AppointmentStatus.Cancelled or AppointmentStatus.Completed))
        {
            return AppointmentResult<AppointmentDto>.Failure("Invalid appointment status.");
        }

        appointment.Status = status;
        appointment.ClinicNotes = clinicNotes?.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return AppointmentResult<AppointmentDto>.Success(await ToDto(appointment.Id, cancellationToken));
    }

    private async Task<AppointmentDto> ToDto(Guid appointmentId, CancellationToken cancellationToken) =>
        (await ToDtos(db.Appointments.AsNoTracking().Where(appointment => appointment.Id == appointmentId), cancellationToken)).Single();

    private async Task<List<AppointmentDto>> ToDtos(IQueryable<Appointment> query, CancellationToken cancellationToken)
    {
        var appointments = await query.Join(db.ClinicServices.AsNoTracking(), appointment => appointment.ClinicServiceId,
                service => service.Id, (appointment, service) => new { appointment, service })
            .Select(item => new AppointmentDto(item.appointment.Id, item.appointment.UserId, item.appointment.ClinicId,
                item.appointment.ClinicServiceId, item.service.Name, item.appointment.StartsAtUtc, item.appointment.EndsAtUtc,
                item.appointment.Status, item.appointment.UserNotes, item.appointment.ClinicNotes,
                Array.Empty<AppointmentPetDto>()))
            .OrderBy(item => item.StartsAtUtc)
            .ToListAsync(cancellationToken);

        var appointmentIds = appointments.Select(appointment => appointment.Id).ToArray();
        var pets = await db.AppointmentPets.AsNoTracking()
            .Join(db.Pets.AsNoTracking(), item => item.PetId, pet => pet.Id, (item, pet) => new { item.AppointmentId, pet })
            .Where(item => appointmentIds.Contains(item.AppointmentId))
            .Select(item => new { item.AppointmentId, Pet = new AppointmentPetDto(item.pet.Id, item.pet.Name, item.pet.Species) })
            .ToListAsync(cancellationToken);

        return appointments
            .Select(appointment => appointment with
            {
                Pets = pets.Where(item => item.AppointmentId == appointment.Id)
                    .Select(item => item.Pet)
                    .ToList()
            })
            .ToList();
    }
}
