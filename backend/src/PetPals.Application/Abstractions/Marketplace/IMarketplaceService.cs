using PetPals.Application.DTOs.Marketplace;
using PetPals.Domain.Enums;

namespace PetPals.Application.Abstractions.Marketplace;

public interface IMarketplaceService
{
    Task<IReadOnlyList<ClinicDto>> GetClinicsAsync(CancellationToken cancellationToken = default);
    Task<ClinicDto?> GetClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<ClinicPublicProfileDto?> GetPublicProfileAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicPhotoDto>> GetClinicPhotosAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<ClinicPhotoDto?> AddClinicPhotoAsync(Guid userId, string imageUrl, CancellationToken cancellationToken = default);
    Task<bool> DeleteClinicPhotoAsync(Guid userId, Guid photoId, CancellationToken cancellationToken = default);
    Task<ClinicDto?> SetClinicPrimaryPhotoAsync(Guid userId, Guid photoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicReviewDto>> GetClinicReviewsAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<ClinicReviewDto>> SaveReviewAsync(Guid userId, Guid clinicId, UpsertClinicReviewRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId, CancellationToken cancellationToken = default);
    Task<ClinicDto?> GetMyClinicAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ClinicDto> SaveMyClinicAsync(Guid userId, UpsertClinicRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        Guid? clinicId,
        ProductCategory? category,
        decimal? minPrice,
        decimal? maxPrice,
        bool? verifiedOnly,
        string? search,
        string? sortBy,
        bool? inStockOnly,
        CancellationToken cancellationToken = default);
    Task<ProductDto?> CreateProductAsync(Guid userId, ProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateProductAsync(Guid userId, Guid productId, ProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);

    Task<CartDto> GetCartAsync(Guid buyerUserId, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<CartDto>> AddToCartAsync(Guid buyerUserId, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<CartDto>> UpdateCartItemAsync(Guid buyerUserId, Guid cartItemId, int quantity, CancellationToken cancellationToken = default);
    Task<CartDto?> RemoveCartItemAsync(Guid buyerUserId, Guid cartItemId, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<CheckoutResponse>> CheckoutAsync(Guid buyerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(Guid buyerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetClinicOrdersAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<OrderDto>> UpdateOrderStatusAsync(Guid userId, Guid orderId, OrderStatus status, CancellationToken cancellationToken = default);
}
