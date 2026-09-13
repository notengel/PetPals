using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPals.Application.Abstractions.Marketplace;
using PetPals.Application.DTOs.Marketplace;
using PetPals.Domain.Enums;
using System.Security.Claims;

namespace PetPals.API.Controllers;

[ApiController]
[Authorize]
[Route("api/marketplace")]
public sealed class MarketplaceController(IMarketplaceService marketplaceService) : ControllerBase
{
    [HttpGet("clinics")]
    public async Task<ActionResult<IReadOnlyList<ClinicDto>>> GetClinics(CancellationToken cancellationToken)
    {
        return Ok(await marketplaceService.GetClinicsAsync(cancellationToken));
    }

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

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts(
        [FromQuery] Guid? clinicId,
        [FromQuery] ProductCategory? category,
        CancellationToken cancellationToken)
    {
        return Ok(await marketplaceService.GetProductsAsync(clinicId, category, cancellationToken));
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

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
