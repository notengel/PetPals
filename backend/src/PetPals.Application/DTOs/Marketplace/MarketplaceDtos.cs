using System.ComponentModel.DataAnnotations;
using PetPals.Domain.Enums;

namespace PetPals.Application.DTOs.Marketplace;

public sealed record MarketplaceResult<T>(bool Succeeded, T? Data, string? Error)
{
    public static MarketplaceResult<T> Success(T data) => new(true, data, null);
    public static MarketplaceResult<T> Failure(string error) => new(false, default, error);
}

public sealed record ClinicDto(
    Guid Id,
    string Name,
    string? Description,
    string? Phone,
    string? Address,
    double Latitude,
    double Longitude,
    bool IsVerified);

public sealed class UpsertClinicRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [MaxLength(30)]
    public string? Phone { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

public sealed record ProductDto(
    Guid Id,
    Guid ClinicId,
    string ClinicName,
    string Name,
    string? Description,
    ProductCategory Category,
    decimal Price,
    int Stock,
    string? ImageUrl,
    bool IsActive);

public sealed class ProductRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    public ProductCategory Category { get; init; }

    [Range(0, 999999999)]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int Stock { get; init; }

    [MaxLength(500)]
    public string? ImageUrl { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class AddCartItemRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Range(1, 999)]
    public int Quantity { get; init; }
}

public sealed class UpdateCartItemRequest
{
    [Range(1, 999)]
    public int Quantity { get; init; }
}

public sealed record CartItemDto(
    Guid Id,
    Guid ProductId,
    Guid ClinicId,
    string ClinicName,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    int AvailableStock);

public sealed record CartDto(
    Guid? Id,
    IReadOnlyList<CartItemDto> Items,
    decimal Total);

public sealed record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal);

public sealed record OrderDto(
    Guid Id,
    Guid ClinicId,
    string ClinicName,
    OrderStatus Status,
    decimal Total,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemDto> Items);

public sealed record CheckoutResponse(IReadOnlyList<OrderDto> Orders);
