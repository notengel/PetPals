using PetPals.Application.DTOs.Adoptions;

namespace PetPals.Application.Abstractions.Adoptions;

public interface IAdoptionService
{
    Task<IReadOnlyList<AdoptablePetDto>> GetAvailablePetsAsync(CancellationToken cancellationToken = default);
    Task<AdoptablePetDto?> GetPetAsync(Guid petId, CancellationToken cancellationToken = default);
    Task<AdoptionResult<AdoptablePetDto>> CreatePetAsync(Guid userId, CreateAdoptablePetRequest request, CancellationToken cancellationToken = default);
    Task<AdoptionResult<VaccinationRecordDto>> AddVaccinationAsync(Guid userId, Guid petId, CreateVaccinationRecordRequest request, CancellationToken cancellationToken = default);
    Task<AdoptionResult<AdoptionRequestDto>> CreateRequestAsync(Guid userId, Guid petId, CreateAdoptionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdoptionRequestDto>> GetMyRequestsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdoptionRequestDto>> GetShelterRequestsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AdoptionResult<AdoptionRequestDto>> UpdateRequestAsync(Guid userId, Guid requestId, UpdateAdoptionRequest request, CancellationToken cancellationToken = default);
    Task<AdoptionResult<object>> SaveShelterAsync(Guid userId, UpsertShelterRequest request, CancellationToken cancellationToken = default);
}
