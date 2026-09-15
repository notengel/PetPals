using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPals.Application.Abstractions.Marketplace;
using PetPals.Application.Abstractions.Storage;
using PetPals.Application.DTOs.Marketplace;
using PetPals.Domain.Enums;
using System.Security.Claims;

namespace PetPals.API.Controllers;

[ApiController]
[Authorize]
[Route("api/marketplace")]
public sealed class MarketplaceController(IMarketplaceService marketplaceService, IFileStorage files) : ControllerBase
{
    [HttpGet("clinics")]
    public async Task<ActionResult<IReadOnlyList<ClinicDto>>> GetClinics(CancellationToken cancellationToken)
    {
        return Ok(await marketplaceService.GetClinicsAsync(cancellationToken));
    }

    [HttpGet("clinics/{clinicId:guid}")]
    public async Task<ActionResult<ClinicDto>> GetClinic(Guid clinicId, CancellationToken cancellationToken)
    {
        var clinic = await marketplaceService.GetClinicAsync(clinicId, cancellationToken);
        return clinic is null ? NotFound() : Ok(clinic);
    }

    [HttpGet("clinics/{clinicId:guid}/profile")]
    public async Task<ActionResult<ClinicPublicProfileDto>> GetClinicProfile(Guid clinicId, CancellationToken cancellationToken)
    {
        var profile = await marketplaceService.GetPublicProfileAsync(clinicId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("clinics/{clinicId:guid}/photos")]
    public async Task<ActionResult<IReadOnlyList<ClinicPhotoDto>>> GetClinicPhotos(Guid clinicId, CancellationToken cancellationToken) =>
        Ok(await marketplaceService.GetClinicPhotosAsync(clinicId, cancellationToken));

    [HttpPost("clinics/me/photos")]
    [Authorize(Roles = "Clinic")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ClinicPhotoDto>> AddClinicPhoto(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var url = await files.SaveAsync(stream, file.FileName, file.ContentType, cancellationToken);
        var photo = await marketplaceService.AddClinicPhotoAsync(CurrentUserId(), url, cancellationToken);
        return photo is null ? BadRequest("Create your clinic profile first.") : Ok(photo);
    }

    [HttpDelete("clinics/me/photos/{photoId:guid}")]
    [Authorize(Roles = "Clinic")]
    public async Task<IActionResult> DeleteClinicPhoto(Guid photoId, CancellationToken cancellationToken) =>
        await marketplaceService.DeleteClinicPhotoAsync(CurrentUserId(), photoId, cancellationToken) ? NoContent() : NotFound();

    [HttpPut("clinics/me/primary")]
    [Authorize(Roles = "Clinic")]
    public async Task<ActionResult<ClinicDto>> SetClinicPrimaryPhoto(SetClinicPrimaryPhotoRequest request, CancellationToken cancellationToken)
    {
        var clinic = await marketplaceService.SetClinicPrimaryPhotoAsync(CurrentUserId(), request.PhotoId, cancellationToken);
        return clinic is null ? NotFound() : Ok(clinic);
    }

    [HttpGet("clinics/{clinicId:guid}/reviews")]
    public async Task<ActionResult<IReadOnlyList<ClinicReviewDto>>> GetClinicReviews(Guid clinicId, CancellationToken cancellationToken) =>
        Ok(await marketplaceService.GetClinicReviewsAsync(clinicId, cancellationToken));

    [HttpPost("clinics/{clinicId:guid}/reviews")]
    public async Task<ActionResult<ClinicReviewDto>> SaveReview(Guid clinicId, UpsertClinicReviewRequest request, CancellationToken cancellationToken)
    {
        var result = await marketplaceService.SaveReviewAsync(CurrentUserId(), clinicId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpDelete("reviews/{reviewId:guid}")]
    public async Task<IActionResult> DeleteReview(Guid reviewId, CancellationToken cancellationToken) =>
        await marketplaceService.DeleteReviewAsync(CurrentUserId(), reviewId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("clinics/me")]
    [Authorize(Roles = "Clinic")]
    public async Task<ActionResult<ClinicDto>> GetMyClinic(CancellationToken cancellationToken)
    {
        var clinic = await marketplaceService.GetMyClinicAsync(CurrentUserId(), cancellationToken);
        return clinic is null ? NotFound("Create your clinic profile first.") : Ok(clinic);
    }

    [HttpPut("clinics/me")]
    [Authorize(Roles = "Clinic")]
    public async Task<ActionResult<ClinicDto>> SaveMyClinic(
        UpsertClinicRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await marketplaceService.SaveMyClinicAsync(CurrentUserId(), request, cancellationToken));
    }

    [HttpPost("clinics/me/logo")]
    [Authorize(Roles = "Clinic")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ClinicDto>> UploadClinicLogo(IFormFile file, CancellationToken cancellationToken)
        => Ok(await SaveClinicPhotoAsync(CurrentUserId(), file, isLogo: true, cancellationToken));

    [HttpPost("clinics/me/banner")]
    [Authorize(Roles = "Clinic")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ClinicDto>> UploadClinicBanner(IFormFile file, CancellationToken cancellationToken)
        => Ok(await SaveClinicPhotoAsync(CurrentUserId(), file, isLogo: false, cancellationToken));

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts(
        [FromQuery] Guid? clinicId,
        [FromQuery] ProductCategory? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] bool? verifiedOnly,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] bool? inStockOnly,
        CancellationToken cancellationToken = default)
    {
        return Ok(await marketplaceService.GetProductsAsync(
            clinicId, category, minPrice, maxPrice, verifiedOnly, search, sortBy, inStockOnly, cancellationToken));
    }

    [HttpPost("products")]
    [Authorize(Roles = "Clinic")]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await marketplaceService.CreateProductAsync(CurrentUserId(), request, cancellationToken);
        return product is null
            ? BadRequest("Create your clinic profile first and use a valid category.")
            : Ok(product);
    }

    [HttpPut("products/{productId:guid}")]
    [Authorize(Roles = "Clinic")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        Guid productId,
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await marketplaceService.UpdateProductAsync(
            CurrentUserId(), productId, request, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpDelete("products/{productId:guid}")]
    [Authorize(Roles = "Clinic")]
    public async Task<IActionResult> DeleteProduct(Guid productId, CancellationToken cancellationToken)
    {
        return await marketplaceService.DeleteProductAsync(CurrentUserId(), productId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpGet("cart")]
    [Authorize(Roles = "User,Shelter")]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken cancellationToken)
    {
        return Ok(await marketplaceService.GetCartAsync(CurrentUserId(), cancellationToken));
    }

    [HttpPost("cart/items")]
    [Authorize(Roles = "User,Shelter")]
    public async Task<ActionResult<CartDto>> AddCartItem(
        AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await marketplaceService.AddToCartAsync(CurrentUserId(), request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPut("cart/items/{cartItemId:guid}")]
    [Authorize(Roles = "User,Shelter")]
    public async Task<ActionResult<CartDto>> UpdateCartItem(
        Guid cartItemId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await marketplaceService.UpdateCartItemAsync(
            CurrentUserId(), cartItemId, request.Quantity, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpDelete("cart/items/{cartItemId:guid}")]
    [Authorize(Roles = "User,Shelter")]
    public async Task<ActionResult<CartDto>> RemoveCartItem(
        Guid cartItemId,
        CancellationToken cancellationToken)
    {
        var cart = await marketplaceService.RemoveCartItemAsync(
            CurrentUserId(), cartItemId, cancellationToken);
        return cart is null ? NotFound() : Ok(cart);
    }

    [HttpPost("cart/checkout")]
    [Authorize(Roles = "User,Shelter")]
    public async Task<ActionResult<CheckoutResponse>> Checkout(CancellationToken cancellationToken)
    {
        var result = await marketplaceService.CheckoutAsync(CurrentUserId(), cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("orders")]
    [Authorize(Roles = "User,Shelter")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetMyOrders(CancellationToken cancellationToken)
    {
        return Ok(await marketplaceService.GetMyOrdersAsync(CurrentUserId(), cancellationToken));
    }

    [HttpGet("clinic/orders")]
    [Authorize(Roles = "Clinic")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetClinicOrders(CancellationToken cancellationToken) =>
        Ok(await marketplaceService.GetClinicOrdersAsync(CurrentUserId(), cancellationToken));

    [HttpPut("clinic/orders/{orderId:guid}/status")]
    [Authorize(Roles = "Clinic")]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(
        Guid orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await marketplaceService.UpdateOrderStatusAsync(CurrentUserId(), orderId, request.Status, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    private async Task<ClinicDto> SaveClinicPhotoAsync(Guid userId, IFormFile file, bool isLogo, CancellationToken cancellationToken)
    {
        var current = await marketplaceService.GetMyClinicAsync(userId, cancellationToken)
            ?? new ClinicDto(Guid.Empty, string.Empty, null, null, null, null, null, 0, 0, false);
        await using var stream = file.OpenReadStream();
        var url = await files.SaveAsync(stream, file.FileName, file.ContentType, cancellationToken);
        return await marketplaceService.SaveMyClinicAsync(userId, new UpsertClinicRequest
        {
            Name = current.Name, Description = current.Description, Phone = current.Phone, Address = current.Address,
            LogoUrl = isLogo ? url : current.LogoUrl, BannerUrl = isLogo ? current.BannerUrl : url,
            Latitude = current.Latitude, Longitude = current.Longitude
        }, cancellationToken);
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
