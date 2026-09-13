using PetPals.Application.DTOs.Appointments;

namespace PetPals.Application.Abstractions.Appointments;

public interface IAppointmentService
{
    Task<IReadOnlyList<ClinicServiceDto>> GetServicesAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicScheduleDto>> GetSchedulesAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<ClinicServiceDto?> CreateServiceAsync(Guid userId, CreateClinicServiceRequest request, CancellationToken cancellationToken = default);
    Task<ClinicScheduleDto?> CreateScheduleAsync(Guid userId, CreateClinicScheduleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentDto>> GetMyAppointmentsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentDto>> GetClinicAppointmentsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AppointmentResult<AppointmentDto>> CreateAppointmentAsync(Guid userId, CreateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<AppointmentResult<AppointmentDto>> CancelAppointmentAsync(Guid userId, Guid appointmentId, CancellationToken cancellationToken = default);
    Task<AppointmentResult<AppointmentDto>> UpdateStatusAsync(Guid userId, Guid appointmentId, UpdateAppointmentStatusRequest request, CancellationToken cancellationToken = default);
}
