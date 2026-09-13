using PetPals.Application.DTOs.Marketplace;
using PetPals.Domain.Enums;

namespace PetPals.Application.Abstractions.Marketplace;

public interface IMarketplaceService
{
    Task<IReadOnlyList<ClinicDto>> GetClinicsAsync(CancellationToken cancellationToken = default);
    Task<ClinicDto?> GetMyClinicAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ClinicDto> SaveMyClinicAsync(Guid userId, UpsertClinicRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDto>> GetProductsAsync(Guid? clinicId, ProductCategory? category, CancellationToken cancellationToken = default);
    Task<ProductDto?> CreateProductAsync(Guid userId, ProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateProductAsync(Guid userId, Guid productId, ProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);

    Task<CartDto> GetCartAsync(Guid buyerUserId, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<CartDto>> AddToCartAsync(Guid buyerUserId, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<CartDto>> UpdateCartItemAsync(Guid buyerUserId, Guid cartItemId, int quantity, CancellationToken cancellationToken = default);
    Task<CartDto?> RemoveCartItemAsync(Guid buyerUserId, Guid cartItemId, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<CheckoutResponse>> CheckoutAsync(Guid buyerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(Guid buyerUserId, CancellationToken cancellationToken = default);
}
