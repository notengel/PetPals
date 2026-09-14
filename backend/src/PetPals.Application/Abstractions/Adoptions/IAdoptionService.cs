using PetPals.Application.DTOs.Adoptions;

namespace PetPals.Application.Abstractions.Adoptions;

public interface IAdoptionService
{
    Task<IReadOnlyList<AdoptablePetDto>> GetAvailablePetsAsync(CancellationToken cancellationToken = default);
    Task<AdoptablePetDto?> GetPetAsync(Guid petId, CancellationToken cancellationToken = default);
    Task<AdoptionResult<AdoptablePetDto>> CreatePetAsync(Guid userId, CreateAdoptablePetRequest request, CancellationToken cancellationToken = default);
    Task<AdoptionResult<VaccinationRecordDto>> AddVaccinationAsync(Guid userId, Guid petId, CreateVaccinationRecordRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdoptablePetPhotoDto>> GetPetPhotosAsync(Guid petId, CancellationToken cancellationToken = default);
    Task<AdoptablePetPhotoDto?> AddPetPhotoAsync(Guid userId, Guid petId, string imageUrl, CancellationToken cancellationToken = default);
    Task<bool> DeletePetPhotoAsync(Guid userId, Guid petId, Guid photoId, CancellationToken cancellationToken = default);
    Task<AdoptablePetDto?> SetPetPrimaryPhotoAsync(Guid userId, Guid petId, Guid photoId, CancellationToken cancellationToken = default);
    Task<AdoptionResult<AdoptionRequestDto>> CreateRequestAsync(Guid userId, Guid petId, CreateAdoptionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdoptionRequestDto>> GetMyRequestsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdoptionRequestDto>> GetShelterRequestsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AdoptionResult<AdoptionRequestDto>> UpdateRequestAsync(Guid userId, Guid requestId, UpdateAdoptionRequest request, CancellationToken cancellationToken = default);
    Task<ShelterDto?> GetMyShelterAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AdoptionResult<ShelterDto>> SaveShelterAsync(Guid userId, UpsertShelterRequest request, CancellationToken cancellationToken = default);
}
