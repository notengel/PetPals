using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPals.Application.Abstractions.Adoptions;
using PetPals.Application.DTOs.Adoptions;
using System.Security.Claims;

namespace PetPals.API.Controllers;

[ApiController]
[Authorize]
[Route("api/adoptions")]
public sealed class AdoptionsController(IAdoptionService adoptionService) : ControllerBase
{
    [HttpGet("pets")]
    public async Task<ActionResult<IReadOnlyList<AdoptablePetDto>>> GetPets(CancellationToken cancellationToken) =>
        Ok(await adoptionService.GetAvailablePetsAsync(cancellationToken));

    [HttpGet("pets/{petId:guid}")]
    public async Task<ActionResult<AdoptablePetDto>> GetPet(Guid petId, CancellationToken cancellationToken)
    {
        var pet = await adoptionService.GetPetAsync(petId, cancellationToken);
        return pet is null ? NotFound() : Ok(pet);
    }

    [HttpPut("shelter")]
    [Authorize(Roles = "Shelter")]
    public async Task<IActionResult> SaveShelter(UpsertShelterRequest request, CancellationToken cancellationToken)
    {
        var result = await adoptionService.SaveShelterAsync(CurrentUserId(), request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("pets")]
    [Authorize(Roles = "Shelter")]
    public async Task<ActionResult<AdoptablePetDto>> CreatePet(CreateAdoptablePetRequest request, CancellationToken cancellationToken)
    {
        var result = await adoptionService.CreatePetAsync(CurrentUserId(), request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("pets/{petId:guid}/vaccinations")]
    [Authorize(Roles = "Shelter")]
    public async Task<ActionResult<VaccinationRecordDto>> AddVaccination(Guid petId, CreateVaccinationRecordRequest request, CancellationToken cancellationToken)
    {
        var result = await adoptionService.AddVaccinationAsync(CurrentUserId(), petId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("pets/{petId:guid}/requests")]
    [Authorize(Roles = "User,Shelter")]
    public async Task<ActionResult<AdoptionRequestDto>> CreateRequest(Guid petId, CreateAdoptionRequest request, CancellationToken cancellationToken)
    {
        var result = await adoptionService.CreateRequestAsync(CurrentUserId(), petId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("requests/mine")]
    [Authorize(Roles = "User,Shelter")]
    public async Task<ActionResult<IReadOnlyList<AdoptionRequestDto>>> GetMine(CancellationToken cancellationToken) =>
        Ok(await adoptionService.GetMyRequestsAsync(CurrentUserId(), cancellationToken));

    [HttpGet("requests/shelter")]
    [Authorize(Roles = "Shelter")]
    public async Task<ActionResult<IReadOnlyList<AdoptionRequestDto>>> GetShelterRequests(CancellationToken cancellationToken) =>
        Ok(await adoptionService.GetShelterRequestsAsync(CurrentUserId(), cancellationToken));

    [HttpPut("requests/{requestId:guid}")]
    [Authorize(Roles = "Shelter")]
    public async Task<ActionResult<AdoptionRequestDto>> UpdateRequest(Guid requestId, UpdateAdoptionRequest request, CancellationToken cancellationToken)
    {
        var result = await adoptionService.UpdateRequestAsync(CurrentUserId(), requestId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
