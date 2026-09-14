using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPals.Application.Abstractions.Adoptions;
using PetPals.Application.Abstractions.Storage;
using PetPals.Application.DTOs.Adoptions;
using System.Security.Claims;

namespace PetPals.API.Controllers;

[ApiController]
[Authorize]
[Route("api/adoptions")]
public sealed class AdoptionsController(IAdoptionService adoptionService, IFileStorage files) : ControllerBase
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

    [HttpGet("shelter/me")]
    [Authorize(Roles = "Shelter")]
    public async Task<ActionResult<ShelterDto>> GetMyShelter(CancellationToken cancellationToken)
    {
        var shelter = await adoptionService.GetMyShelterAsync(CurrentUserId(), cancellationToken);
        return shelter is null ? NotFound("Create your shelter profile first.") : Ok(shelter);
    }

    [HttpPut("shelter")]
    [Authorize(Roles = "Shelter")]
    public async Task<IActionResult> SaveShelter(UpsertShelterRequest request, CancellationToken cancellationToken)
    {
        var result = await adoptionService.SaveShelterAsync(CurrentUserId(), request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("shelter/me/logo")]
    [Authorize(Roles = "Shelter")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ShelterDto>> UploadShelterLogo(IFormFile file, CancellationToken cancellationToken)
    {
        var result = await SaveShelterPhotoAsync(CurrentUserId(), file, isLogo: true, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("shelter/me/banner")]
    [Authorize(Roles = "Shelter")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ShelterDto>> UploadShelterBanner(IFormFile file, CancellationToken cancellationToken)
    {
        var result = await SaveShelterPhotoAsync(CurrentUserId(), file, isLogo: false, cancellationToken);
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
    [Authorize(Roles = "User,Clinic,Shelter")]
    public async Task<ActionResult<AdoptionRequestDto>> CreateRequest(Guid petId, CreateAdoptionRequest request, CancellationToken cancellationToken)
    {
        var result = await adoptionService.CreateRequestAsync(CurrentUserId(), petId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("requests/mine")]
    [Authorize(Roles = "User,Clinic,Shelter")]
    public async Task<ActionResult<IReadOnlyList<AdoptionRequestDto>>> GetMine(CancellationToken cancellationToken) =>
        Ok(await adoptionService.GetMyRequestsAsync(CurrentUserId(), cancellationToken));

    [HttpGet("requests/shelter")]
    [Authorize(Roles = "Shelter")]
    public async Task<ActionResult<IReadOnlyList<AdoptionRequestDto>>> GetShelterRequests(CancellationToken cancellationToken) =>
        Ok(await adoptionService.GetShelterRequestsAsync(CurrentUserId(), cancellationToken));

    [HttpGet("pets/{petId:guid}/photos")]
    public async Task<ActionResult<IReadOnlyList<AdoptablePetPhotoDto>>> GetPetPhotos(Guid petId, CancellationToken cancellationToken) =>
        Ok(await adoptionService.GetPetPhotosAsync(petId, cancellationToken));

    [HttpPost("pets/{petId:guid}/photos")]
    [Authorize(Roles = "Shelter")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<AdoptablePetPhotoDto>> AddPetPhoto(Guid petId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var url = await files.SaveAsync(stream, file.FileName, file.ContentType, cancellationToken);
        var photo = await adoptionService.AddPetPhotoAsync(CurrentUserId(), petId, url, cancellationToken);
        return photo is null ? NotFound() : Ok(photo);
    }

    [HttpDelete("pets/{petId:guid}/photos/{photoId:guid}")]
    [Authorize(Roles = "Shelter")]
    public async Task<IActionResult> DeletePetPhoto(Guid petId, Guid photoId, CancellationToken cancellationToken) =>
        await adoptionService.DeletePetPhotoAsync(CurrentUserId(), petId, photoId, cancellationToken) ? NoContent() : NotFound();

    [HttpPut("pets/{petId:guid}/primary")]
    [Authorize(Roles = "Shelter")]
    public async Task<ActionResult<AdoptablePetDto>> SetPetPrimaryPhoto(Guid petId, SetAdoptablePetPrimaryPhotoRequest request, CancellationToken cancellationToken)
    {
        var pet = await adoptionService.SetPetPrimaryPhotoAsync(CurrentUserId(), petId, request.PhotoId, cancellationToken);
        return pet is null ? NotFound() : Ok(pet);
    }

    [HttpPut("requests/{requestId:guid}")]
    [Authorize(Roles = "Shelter")]
    public async Task<ActionResult<AdoptionRequestDto>> UpdateRequest(Guid requestId, UpdateAdoptionRequest request, CancellationToken cancellationToken)
    {
        var result = await adoptionService.UpdateRequestAsync(CurrentUserId(), requestId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    private async Task<AdoptionResult<ShelterDto>> SaveShelterPhotoAsync(Guid userId, IFormFile file, bool isLogo, CancellationToken cancellationToken)
    {
        var current = await adoptionService.GetMyShelterAsync(userId, cancellationToken);
        if (current is null) return AdoptionResult<ShelterDto>.Failure("Create your shelter profile first.");
        await using var stream = file.OpenReadStream();
        var url = await files.SaveAsync(stream, file.FileName, file.ContentType, cancellationToken);
        return await adoptionService.SaveShelterAsync(userId, new UpsertShelterRequest
        {
            Name = current.Name, Description = current.Description, Phone = current.Phone, Address = current.Address,
            LogoUrl = isLogo ? url : current.LogoUrl, BannerUrl = isLogo ? current.BannerUrl : url,
            Latitude = current.Latitude, Longitude = current.Longitude
        }, cancellationToken);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
