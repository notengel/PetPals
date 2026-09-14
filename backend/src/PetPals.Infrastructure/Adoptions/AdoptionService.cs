using Microsoft.EntityFrameworkCore;
using PetPals.Application.Abstractions.Adoptions;
using PetPals.Application.DTOs.Adoptions;
using PetPals.Domain.Entities;
using PetPals.Domain.Enums;
using PetPals.Infrastructure.Persistence;

namespace PetPals.Infrastructure.Adoptions;

public sealed class AdoptionService(ApplicationDbContext db) : IAdoptionService
{
    public async Task<IReadOnlyList<AdoptablePetDto>> GetAvailablePetsAsync(CancellationToken cancellationToken = default) =>
        await ToPetDtos(db.AdoptablePets.AsNoTracking().Where(pet => pet.Status == AdoptablePetStatus.Available), cancellationToken);

    public async Task<AdoptablePetDto?> GetPetAsync(Guid petId, CancellationToken cancellationToken = default)
    {
        var pets = await ToPetDtos(db.AdoptablePets.AsNoTracking().Where(pet => pet.Id == petId), cancellationToken);
        return pets.SingleOrDefault();
    }

    public async Task<AdoptionResult<AdoptablePetDto>> CreatePetAsync(Guid userId, CreateAdoptablePetRequest request, CancellationToken cancellationToken = default)
    {
        var shelter = await db.Shelters.SingleOrDefaultAsync(shelter => shelter.OwnerUserId == userId, cancellationToken);
        if (shelter is null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Species))
        {
            return AdoptionResult<AdoptablePetDto>.Failure("Create your shelter profile and provide the pet's name and species.");
        }

        var pet = new AdoptablePet
        {
            ShelterId = shelter.Id,
            Name = request.Name.Trim(),
            Species = request.Species.Trim(),
            Breed = request.Breed?.Trim(),
            Sex = request.Sex?.Trim(),
            ApproximateAge = request.ApproximateAge?.Trim(),
            Description = request.Description?.Trim(),
            PrimaryImageUrl = request.PrimaryImageUrl?.Trim()
        };
        db.AdoptablePets.Add(pet);
        await db.SaveChangesAsync(cancellationToken);
        return AdoptionResult<AdoptablePetDto>.Success((await ToPetDtos(db.AdoptablePets.AsNoTracking().Where(item => item.Id == pet.Id), cancellationToken)).Single());
    }

    public async Task<AdoptionResult<VaccinationRecordDto>> AddVaccinationAsync(Guid userId, Guid petId, CreateVaccinationRecordRequest request, CancellationToken cancellationToken = default)
    {
        var ownsPet = await (from pet in db.AdoptablePets
                             join shelter in db.Shelters on pet.ShelterId equals shelter.Id
                             where pet.Id == petId && shelter.OwnerUserId == userId
                             select pet.Id).AnyAsync(cancellationToken);
        if (!ownsPet || string.IsNullOrWhiteSpace(request.VaccineName))
        {
            return AdoptionResult<VaccinationRecordDto>.Failure("The adoptable pet was not found.");
        }

        var record = new VaccinationRecord
        {
            AdoptablePetId = petId,
            VaccineName = request.VaccineName.Trim(),
            AppliedOn = request.AppliedOn,
            NextDueOn = request.NextDueOn,
            Notes = request.Notes?.Trim()
        };
        db.VaccinationRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return AdoptionResult<VaccinationRecordDto>.Success(ToDto(record));
    }

    public async Task<AdoptionResult<AdoptionRequestDto>> CreateRequestAsync(Guid userId, Guid petId, CreateAdoptionRequest request, CancellationToken cancellationToken = default)
    {
        var pet = await db.AdoptablePets.SingleOrDefaultAsync(item => item.Id == petId && item.Status == AdoptablePetStatus.Available, cancellationToken);
        var alreadyRequested = await db.AdoptionRequests.AnyAsync(item => item.AdoptablePetId == petId && item.ApplicantUserId == userId &&
            (item.Status == AdoptionRequestStatus.Pending || item.Status == AdoptionRequestStatus.UnderReview || item.Status == AdoptionRequestStatus.Approved), cancellationToken);
        if (pet is null || alreadyRequested)
        {
            return AdoptionResult<AdoptionRequestDto>.Failure("The pet is unavailable or you already have an active request.");
        }

        var adoptionRequest = new AdoptionRequest
        {
            AdoptablePetId = petId,
            ApplicantUserId = userId,
            ApplicantMessage = request.ApplicantMessage?.Trim()
        };
        db.AdoptionRequests.Add(adoptionRequest);
        await db.SaveChangesAsync(cancellationToken);
        return AdoptionResult<AdoptionRequestDto>.Success(ToDto(adoptionRequest, pet.Name));
    }

    public Task<IReadOnlyList<AdoptionRequestDto>> GetMyRequestsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        GetRequestsAsync(db.AdoptionRequests.AsNoTracking().Where(request => request.ApplicantUserId == userId), cancellationToken);

    public async Task<IReadOnlyList<AdoptionRequestDto>> GetShelterRequestsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var shelterId = await db.Shelters.Where(shelter => shelter.OwnerUserId == userId)
            .Select(shelter => (Guid?)shelter.Id).SingleOrDefaultAsync(cancellationToken);
        return shelterId is null ? [] : await GetRequestsAsync(
            from request in db.AdoptionRequests.AsNoTracking()
            join pet in db.AdoptablePets.AsNoTracking() on request.AdoptablePetId equals pet.Id
            where pet.ShelterId == shelterId
            select request, cancellationToken);
    }

    public async Task<AdoptionResult<AdoptionRequestDto>> UpdateRequestAsync(Guid userId, Guid requestId, UpdateAdoptionRequest request, CancellationToken cancellationToken = default)
    {
        var adoptionRequest = await (from current in db.AdoptionRequests
                                     join pet in db.AdoptablePets on current.AdoptablePetId equals pet.Id
                                     join shelter in db.Shelters on pet.ShelterId equals shelter.Id
                                     where current.Id == requestId && shelter.OwnerUserId == userId
                                     select new { Request = current, Pet = pet }).SingleOrDefaultAsync(cancellationToken);
        if (adoptionRequest is null || request.Status == AdoptionRequestStatus.Pending || request.Status == AdoptionRequestStatus.UnderReview || request.Status == AdoptionRequestStatus.Cancelled)
        {
            return AdoptionResult<AdoptionRequestDto>.Failure("The request or status is invalid.");
        }

        adoptionRequest.Request.Status = request.Status;
        adoptionRequest.Request.ShelterComment = request.ShelterComment?.Trim();
        adoptionRequest.Request.ResolvedAtUtc = request.Status is AdoptionRequestStatus.Approved or AdoptionRequestStatus.Rejected
            ? DateTime.UtcNow : null;
        if (request.Status == AdoptionRequestStatus.Approved)
        {
            adoptionRequest.Pet.Status = AdoptablePetStatus.Adopted;
            await db.AdoptionRequests
                .Where(item => item.AdoptablePetId == adoptionRequest.Pet.Id && item.Id != adoptionRequest.Request.Id &&
                    (item.Status == AdoptionRequestStatus.Pending || item.Status == AdoptionRequestStatus.UnderReview))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Status, AdoptionRequestStatus.Rejected)
                    .SetProperty(item => item.ResolvedAtUtc, DateTime.UtcNow), cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return AdoptionResult<AdoptionRequestDto>.Success(ToDto(adoptionRequest.Request, adoptionRequest.Pet.Name));
    }

    public async Task<ShelterDto?> GetMyShelterAsync(Guid userId, CancellationToken cancellationToken = default)
{
        var shelter = await db.Shelters.AsNoTracking().SingleOrDefaultAsync(s => s.OwnerUserId == userId, cancellationToken);
        return shelter is null ? null : ToShelterDto(shelter);
    }

    public async Task<AdoptionResult<ShelterDto>> SaveShelterAsync(Guid userId, UpsertShelterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return AdoptionResult<ShelterDto>.Failure("Shelter name is required.");
        }

        var shelter = await db.Shelters.SingleOrDefaultAsync(item => item.OwnerUserId == userId, cancellationToken);
        if (shelter is null)
        {
            shelter = new Shelter { OwnerUserId = userId };
            db.Shelters.Add(shelter);
        }

        shelter.Name = request.Name.Trim();
        shelter.Description = request.Description?.Trim();
        shelter.Phone = request.Phone?.Trim();
        shelter.Address = request.Address?.Trim();
        shelter.LogoUrl = request.LogoUrl?.Trim();
        shelter.BannerUrl = request.BannerUrl?.Trim();
        shelter.Latitude = request.Latitude;
        shelter.Longitude = request.Longitude;
        await db.SaveChangesAsync(cancellationToken);
        return AdoptionResult<ShelterDto>.Success(ToShelterDto(shelter));
    }

    public async Task<IReadOnlyList<AdoptablePetPhotoDto>> GetPetPhotosAsync(Guid petId, CancellationToken cancellationToken = default) =>
        await db.AdoptablePetPhotos.AsNoTracking().Where(photo => photo.AdoptablePetId == petId)
            .OrderByDescending(photo => photo.CreatedAtUtc)
            .Select(photo => new AdoptablePetPhotoDto(photo.Id, photo.AdoptablePetId, photo.ImageUrl, photo.CreatedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<AdoptablePetPhotoDto?> AddPetPhotoAsync(Guid userId, Guid petId, string imageUrl, CancellationToken cancellationToken = default)
    {
        var pet = await OwnsAdoptablePetAsync(userId, petId, cancellationToken);
        if (pet is null) return null;
        var photo = new AdoptablePetPhoto { AdoptablePetId = petId, ImageUrl = imageUrl.Trim() };
        db.AdoptablePetPhotos.Add(photo);
        if (string.IsNullOrWhiteSpace(pet.PrimaryImageUrl)) pet.PrimaryImageUrl = photo.ImageUrl; // ponytail: primera foto = perfil
        await db.SaveChangesAsync(cancellationToken);
        return new AdoptablePetPhotoDto(photo.Id, photo.AdoptablePetId, photo.ImageUrl, photo.CreatedAtUtc);
    }

    public async Task<bool> DeletePetPhotoAsync(Guid userId, Guid petId, Guid photoId, CancellationToken cancellationToken = default)
    {
        if (await OwnsAdoptablePetAsync(userId, petId, cancellationToken) is null) return false;
        var photo = await db.AdoptablePetPhotos.SingleOrDefaultAsync(p => p.Id == photoId && p.AdoptablePetId == petId, cancellationToken);
        if (photo is null) return false;
        db.AdoptablePetPhotos.Remove(photo);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AdoptablePetDto?> SetPetPrimaryPhotoAsync(Guid userId, Guid petId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var pet = await OwnsAdoptablePetAsync(userId, petId, cancellationToken);
        var photoExists = await db.AdoptablePetPhotos.AnyAsync(p => p.Id == photoId && p.AdoptablePetId == petId, cancellationToken);
        if (pet is null || !photoExists) return null;
        pet.PrimaryImageUrl = (await db.AdoptablePetPhotos.AsNoTracking().SingleAsync(p => p.Id == photoId, cancellationToken)).ImageUrl;
        await db.SaveChangesAsync(cancellationToken);
        return (await ToPetDtos(db.AdoptablePets.AsNoTracking().Where(item => item.Id == petId), cancellationToken)).SingleOrDefault();
    }

    private async Task<AdoptablePet?> OwnsAdoptablePetAsync(Guid userId, Guid petId, CancellationToken cancellationToken) =>
        await (from pet in db.AdoptablePets
               join shelter in db.Shelters on pet.ShelterId equals shelter.Id
               where pet.Id == petId && shelter.OwnerUserId == userId
               select pet).SingleOrDefaultAsync(cancellationToken);

    private async Task<List<AdoptablePetDto>> ToPetDtos(IQueryable<AdoptablePet> query, CancellationToken cancellationToken)
    {
        var pets = await query.Join(db.Shelters.AsNoTracking(), pet => pet.ShelterId, shelter => shelter.Id,
                (pet, shelter) => new { pet, shelter })
            .Select(item => new AdoptablePetDto(item.pet.Id, item.pet.ShelterId, item.shelter.Name, item.pet.Name,
                item.pet.Species, item.pet.Breed, item.pet.Sex, item.pet.ApproximateAge, item.pet.Description,
                item.pet.Status, item.pet.PrimaryImageUrl, Array.Empty<VaccinationRecordDto>(), Array.Empty<AdoptablePetPhotoDto>()))
            .ToListAsync(cancellationToken);
        var ids = pets.Select(pet => pet.Id).ToArray();
        var vaccinations = await db.VaccinationRecords.AsNoTracking().Where(record => ids.Contains(record.AdoptablePetId))
            .ToListAsync(cancellationToken);
        var photos = await db.AdoptablePetPhotos.AsNoTracking().Where(photo => ids.Contains(photo.AdoptablePetId))
            .OrderByDescending(photo => photo.CreatedAtUtc).ToListAsync(cancellationToken);
        return pets.Select(pet => pet with
        {
            Vaccinations = vaccinations.Where(record => record.AdoptablePetId == pet.Id).Select(ToDto).ToList(),
            Photos = photos.Where(photo => photo.AdoptablePetId == pet.Id)
                .Select(photo => new AdoptablePetPhotoDto(photo.Id, photo.AdoptablePetId, photo.ImageUrl, photo.CreatedAtUtc)).ToList()
        }).ToList();
    }

    private async Task<IReadOnlyList<AdoptionRequestDto>> GetRequestsAsync(IQueryable<AdoptionRequest> query, CancellationToken cancellationToken)
    {
        var rows = await query.Join(db.AdoptablePets.AsNoTracking(), request => request.AdoptablePetId, pet => pet.Id,
                (request, pet) => new { request, pet.Name }).OrderByDescending(item => item.request.CreatedAtUtc).ToListAsync(cancellationToken);
        return rows.Select(item => ToDto(item.request, item.Name)).ToList();
    }

    private static VaccinationRecordDto ToDto(VaccinationRecord record) =>
        new(record.Id, record.AdoptablePetId, record.VaccineName, record.AppliedOn, record.NextDueOn, record.Notes);

    private static ShelterDto ToShelterDto(Shelter shelter) =>
        new(shelter.Id, shelter.Name, shelter.Description, shelter.Phone, shelter.Address, shelter.LogoUrl, shelter.BannerUrl, shelter.Latitude, shelter.Longitude, shelter.IsVerified);

    private static AdoptionRequestDto ToDto(AdoptionRequest request, string petName) =>
        new(request.Id, request.AdoptablePetId, petName, request.ApplicantUserId, request.Status,
            request.ApplicantMessage, request.ShelterComment, request.CreatedAtUtc, request.ResolvedAtUtc);
}
