using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPals.Application.Abstractions.Appointments;
using PetPals.Application.DTOs.Appointments;
using System.Security.Claims;

namespace PetPals.API.Controllers;

[ApiController]
[Authorize]
[Route("api/appointments")]
public sealed class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    [HttpGet("clinics/{clinicId:guid}/services")]
    public async Task<ActionResult<IReadOnlyList<ClinicServiceDto>>> GetServices(Guid clinicId, CancellationToken cancellationToken) =>
        Ok(await appointmentService.GetServicesAsync(clinicId, cancellationToken));

    [HttpGet("clinics/{clinicId:guid}/schedules")]
    public async Task<ActionResult<IReadOnlyList<ClinicScheduleDto>>> GetSchedules(Guid clinicId, CancellationToken cancellationToken) =>
        Ok(await appointmentService.GetSchedulesAsync(clinicId, cancellationToken));

    [HttpPost("clinic/services")]
    [Authorize(Roles = "Clinic")]
    public async Task<ActionResult<ClinicServiceDto>> CreateService(CreateClinicServiceRequest request, CancellationToken cancellationToken)
    {
        var service = await appointmentService.CreateServiceAsync(CurrentUserId(), request, cancellationToken);
        return service is null ? BadRequest("Create a valid clinic service first.") : Ok(service);
    }

    [HttpPost("clinic/schedules")]
    [Authorize(Roles = "Clinic")]
    public async Task<ActionResult<ClinicScheduleDto>> CreateSchedule(CreateClinicScheduleRequest request, CancellationToken cancellationToken)
    {
        var schedule = await appointmentService.CreateScheduleAsync(CurrentUserId(), request, cancellationToken);
        return schedule is null ? BadRequest("The schedule is invalid or overlaps another schedule.") : Ok(schedule);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "User,Shelter")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetMine(CancellationToken cancellationToken) =>
        Ok(await appointmentService.GetMyAppointmentsAsync(CurrentUserId(), cancellationToken));

    [HttpPost]
    [Authorize(Roles = "User,Shelter")]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await appointmentService.CreateAppointmentAsync(CurrentUserId(), request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("{appointmentId:guid}/cancel")]
    [Authorize(Roles = "User,Shelter")]
    public async Task<ActionResult<AppointmentDto>> Cancel(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await appointmentService.CancelAppointmentAsync(CurrentUserId(), appointmentId, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("clinic")]
    [Authorize(Roles = "Clinic")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetClinic(CancellationToken cancellationToken) =>
        Ok(await appointmentService.GetClinicAppointmentsAsync(CurrentUserId(), cancellationToken));

    [HttpPut("{appointmentId:guid}/status")]
    [Authorize(Roles = "Clinic")]
    public async Task<ActionResult<AppointmentDto>> UpdateStatus(Guid appointmentId, UpdateAppointmentStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await appointmentService.UpdateStatusAsync(CurrentUserId(), appointmentId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
