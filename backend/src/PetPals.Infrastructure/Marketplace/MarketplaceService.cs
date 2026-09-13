using Microsoft.EntityFrameworkCore;
using PetPals.Application.Abstractions.Marketplace;
using PetPals.Application.DTOs.Marketplace;
using PetPals.Domain.Entities;
using PetPals.Domain.Enums;
using PetPals.Infrastructure.Persistence;

namespace PetPals.Infrastructure.Marketplace;

public sealed class MarketplaceService(ApplicationDbContext db) : IMarketplaceService
{
    public async Task<IReadOnlyList<ClinicDto>> GetClinicsAsync(
        CancellationToken cancellationToken = default)
    {
        return await db.Clinics.AsNoTracking()
            .OrderBy(clinic => clinic.Name)
            .Select(clinic => new ClinicDto(
                clinic.Id,
                clinic.Name,
                clinic.Description,
                clinic.Phone,
                clinic.Address,
                clinic.Latitude,
                clinic.Longitude,
                clinic.IsVerified))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClinicDto?> GetMyClinicAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var clinic = await db.Clinics.AsNoTracking()
            .SingleOrDefaultAsync(clinic => clinic.OwnerUserId == userId, cancellationToken);
        return clinic is null ? null : ToClinicDto(clinic);
    }

    public async Task<ClinicDto> SaveMyClinicAsync(
        Guid userId,
        UpsertClinicRequest request,
        CancellationToken cancellationToken = default)
    {
        var clinic = await db.Clinics
            .SingleOrDefaultAsync(currentClinic => currentClinic.OwnerUserId == userId, cancellationToken);

        if (clinic is null)
        {
            clinic = new Clinic { OwnerUserId = userId };
            db.Clinics.Add(clinic);
        }

        clinic.Name = request.Name.Trim();
        clinic.Description = request.Description?.Trim();
        clinic.Phone = request.Phone?.Trim();
        clinic.Address = request.Address?.Trim();
        clinic.Latitude = request.Latitude;
        clinic.Longitude = request.Longitude;

        await db.SaveChangesAsync(cancellationToken);
        return ToClinicDto(clinic);
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        Guid? clinicId,
        ProductCategory? category,
        CancellationToken cancellationToken = default)
    {
        var query =
            from product in db.Products.AsNoTracking()
            join clinic in db.Clinics.AsNoTracking() on product.ClinicId equals clinic.Id
            where product.IsActive
            select new { product, clinic };

        if (clinicId is not null)
        {
            query = query.Where(item => item.product.ClinicId == clinicId);
        }

        if (category is not null)
        {
            query = query.Where(item => item.product.Category == category);
        }

        return await query
            .OrderBy(item => item.product.Name)
            .Select(item => new ProductDto(
                item.product.Id,
                item.product.ClinicId,
                item.clinic.Name,
                item.product.Name,
                item.product.Description,
                item.product.Category,
                item.product.Price,
                item.product.Stock,
                item.product.ImageUrl,
                item.product.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto?> CreateProductAsync(
        Guid userId,
        ProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var clinic = await db.Clinics.SingleOrDefaultAsync(
            currentClinic => currentClinic.OwnerUserId == userId,
            cancellationToken);

        if (clinic is null || !Enum.IsDefined(request.Category))
        {
            return null;
        }

        var product = new Product { ClinicId = clinic.Id };
        ApplyProduct(product, request);
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return ToProductDto(product, clinic.Name);
    }

    public async Task<ProductDto?> UpdateProductAsync(
        Guid userId,
        Guid productId,
        ProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await (
            from currentProduct in db.Products
            join clinic in db.Clinics on currentProduct.ClinicId equals clinic.Id
            where currentProduct.Id == productId && clinic.OwnerUserId == userId
            select currentProduct)
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null || !Enum.IsDefined(request.Category))
        {
            return null;
        }

        ApplyProduct(product, request);
        await db.SaveChangesAsync(cancellationToken);
        var clinicName = await db.Clinics
            .Where(clinic => clinic.Id == product.ClinicId)
            .Select(clinic => clinic.Name)
            .SingleAsync(cancellationToken);
        return ToProductDto(product, clinicName);
    }

    public async Task<bool> DeleteProductAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await (
            from currentProduct in db.Products
            join clinic in db.Clinics on currentProduct.ClinicId equals clinic.Id
            where currentProduct.Id == productId && clinic.OwnerUserId == userId
            select currentProduct)
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return false;
        }

        product.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CartDto> GetCartAsync(
        Guid buyerUserId,
        CancellationToken cancellationToken = default)
    {
        var cart = await db.Carts.AsNoTracking()
            .SingleOrDefaultAsync(currentCart => currentCart.BuyerUserId == buyerUserId, cancellationToken);
        return cart is null ? new CartDto(null, [], 0) : await ToCartDto(cart.Id, cancellationToken);
    }

    public async Task<MarketplaceResult<CartDto>> AddToCartAsync(
        Guid buyerUserId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity < 1)
        {
            return MarketplaceResult<CartDto>.Failure("Quantity must be greater than zero.");
        }

        var product = await db.Products.SingleOrDefaultAsync(
            currentProduct => currentProduct.Id == request.ProductId && currentProduct.IsActive,
            cancellationToken);
        if (product is null)
        {
            return MarketplaceResult<CartDto>.Failure("Product was not found or is inactive.");
        }

        var cart = await GetOrCreateCartAsync(buyerUserId, cancellationToken);
        var item = await db.CartItems.SingleOrDefaultAsync(
            currentItem => currentItem.CartId == cart.Id && currentItem.ProductId == product.Id,
            cancellationToken);
        var nextQuantity = (item?.Quantity ?? 0) + request.Quantity;
        if (nextQuantity > product.Stock)
        {
            return MarketplaceResult<CartDto>.Failure("The requested quantity exceeds available stock.");
        }

        if (item is null)
        {
            db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = request.Quantity,
                UnitPrice = product.Price
            });
        }
        else
        {
            item.Quantity = nextQuantity;
            item.UnitPrice = product.Price;
        }

        await db.SaveChangesAsync(cancellationToken);
        return MarketplaceResult<CartDto>.Success(await ToCartDto(cart.Id, cancellationToken));
    }

    public async Task<MarketplaceResult<CartDto>> UpdateCartItemAsync(
        Guid buyerUserId,
        Guid cartItemId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var item = await (
            from currentItem in db.CartItems
            join cart in db.Carts on currentItem.CartId equals cart.Id
            join product in db.Products on currentItem.ProductId equals product.Id
            where currentItem.Id == cartItemId && cart.BuyerUserId == buyerUserId
            select new { currentItem, cart, product })
            .SingleOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return MarketplaceResult<CartDto>.Failure("Cart item was not found.");
        }

        if (quantity < 1 || quantity > item.product.Stock)
        {
            return MarketplaceResult<CartDto>.Failure("The requested quantity exceeds available stock.");
        }

        item.currentItem.Quantity = quantity;
        item.currentItem.UnitPrice = item.product.Price;
        await db.SaveChangesAsync(cancellationToken);
        return MarketplaceResult<CartDto>.Success(await ToCartDto(item.cart.Id, cancellationToken));
    }

    public async Task<CartDto?> RemoveCartItemAsync(
        Guid buyerUserId,
        Guid cartItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await (
            from currentItem in db.CartItems
            join cart in db.Carts on currentItem.CartId equals cart.Id
            where currentItem.Id == cartItemId && cart.BuyerUserId == buyerUserId
            select new { currentItem, cart.Id })
            .SingleOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return null;
        }

        db.CartItems.Remove(item.currentItem);
        await db.SaveChangesAsync(cancellationToken);
        return await ToCartDto(item.Id, cancellationToken);
    }

    public async Task<MarketplaceResult<CheckoutResponse>> CheckoutAsync(
        Guid buyerUserId,
        CancellationToken cancellationToken = default)
    {
        var cart = await db.Carts.SingleOrDefaultAsync(
            currentCart => currentCart.BuyerUserId == buyerUserId,
            cancellationToken);
        if (cart is null)
        {
            return MarketplaceResult<CheckoutResponse>.Failure("The cart is empty.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var items = await (
            from item in db.CartItems
            join product in db.Products on item.ProductId equals product.Id
            join clinic in db.Clinics on product.ClinicId equals clinic.Id
            where item.CartId == cart.Id
            select new { item, product, clinic })
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return MarketplaceResult<CheckoutResponse>.Failure("The cart is empty.");
        }

        var unavailable = items.FirstOrDefault(item => !item.product.IsActive || item.product.Stock < item.item.Quantity);
        if (unavailable is not null)
        {
            return MarketplaceResult<CheckoutResponse>.Failure(
                $"Insufficient stock for product '{unavailable.product.Name}'.");
        }

        var orders = new List<Order>();
        foreach (var clinicItems in items.GroupBy(item => item.clinic.Id))
        {
            var first = clinicItems.First();
            var order = new Order
            {
                BuyerUserId = buyerUserId,
                ClinicId = first.clinic.Id,
                Status = OrderStatus.Pending
            };

            foreach (var row in clinicItems)
            {
                var subtotal = row.product.Price * row.item.Quantity;
                order.Items.Add(new OrderItem
                {
                    ProductId = row.product.Id,
                    ProductName = row.product.Name,
                    UnitPrice = row.product.Price,
                    Quantity = row.item.Quantity,
                    Subtotal = subtotal
                });
                order.Total += subtotal;
                row.product.Stock -= row.item.Quantity;
            }

            orders.Add(order);
            db.Orders.Add(order);
        }

        db.CartItems.RemoveRange(items.Select(item => item.item));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return MarketplaceResult<CheckoutResponse>.Success(new CheckoutResponse(
            orders.Select(order => ToOrderDto(order, items.First(item => item.clinic.Id == order.ClinicId).clinic.Name)).ToList()));
    }

    public async Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(
        Guid buyerUserId,
        CancellationToken cancellationToken = default)
    {
        var orders = await db.Orders.AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.BuyerUserId == buyerUserId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var clinicNames = await db.Clinics.AsNoTracking()
            .Where(clinic => orders.Select(order => order.ClinicId).Contains(clinic.Id))
            .ToDictionaryAsync(clinic => clinic.Id, clinic => clinic.Name, cancellationToken);

        return orders.Select(order => ToOrderDto(order, clinicNames[order.ClinicId])).ToList();
    }

    public async Task<IReadOnlyList<OrderDto>> GetClinicOrdersAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var clinicId = await db.Clinics.Where(clinic => clinic.OwnerUserId == userId)
            .Select(clinic => (Guid?)clinic.Id).SingleOrDefaultAsync(cancellationToken);
        if (clinicId is null)
        {
            return [];
        }

        var clinicName = await db.Clinics.Where(clinic => clinic.Id == clinicId)
            .Select(clinic => clinic.Name).SingleAsync(cancellationToken);
        var orders = await db.Orders.AsNoTracking().Include(order => order.Items)
            .Where(order => order.ClinicId == clinicId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return orders.Select(order => ToOrderDto(order, clinicName)).ToList();
    }

    public async Task<MarketplaceResult<OrderDto>> UpdateOrderStatusAsync(
        Guid userId,
        Guid orderId,
        OrderStatus status,
        CancellationToken cancellationToken = default)
    {
        if (status is not (OrderStatus.Confirmed or OrderStatus.Cancelled or OrderStatus.Completed))
        {
            return MarketplaceResult<OrderDto>.Failure("Invalid order status.");
        }

        var order = await (from currentOrder in db.Orders
                           join clinic in db.Clinics on currentOrder.ClinicId equals clinic.Id
                           where currentOrder.Id == orderId && clinic.OwnerUserId == userId
                           select new { Order = currentOrder, ClinicName = clinic.Name }).SingleOrDefaultAsync(cancellationToken);
        if (order is null)
        {
            return MarketplaceResult<OrderDto>.Failure("Order not found.");
        }

        order.Order.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(order.Order).Collection(currentOrder => currentOrder.Items).LoadAsync(cancellationToken);
        return MarketplaceResult<OrderDto>.Success(ToOrderDto(order.Order, order.ClinicName));
    }

    private async Task<Cart> GetOrCreateCartAsync(Guid buyerUserId, CancellationToken cancellationToken)
    {
        var cart = await db.Carts.SingleOrDefaultAsync(
            currentCart => currentCart.BuyerUserId == buyerUserId,
            cancellationToken);
        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart { BuyerUserId = buyerUserId };
        db.Carts.Add(cart);
        await db.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private async Task<CartDto> ToCartDto(Guid cartId, CancellationToken cancellationToken)
    {
        var items = await (
            from item in db.CartItems.AsNoTracking()
            join product in db.Products.AsNoTracking() on item.ProductId equals product.Id
            join clinic in db.Clinics.AsNoTracking() on product.ClinicId equals clinic.Id
            where item.CartId == cartId
            select new CartItemDto(
                item.Id,
                product.Id,
                clinic.Id,
                clinic.Name,
                product.Name,
                item.UnitPrice,
                item.Quantity,
                item.UnitPrice * item.Quantity,
                product.Stock))
            .ToListAsync(cancellationToken);
        return new CartDto(cartId, items, items.Sum(item => item.Subtotal));
    }

    private static void ApplyProduct(Product product, ProductRequest request)
    {
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Category = request.Category;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.ImageUrl = request.ImageUrl?.Trim();
        product.IsActive = request.IsActive;
    }

    private static ClinicDto ToClinicDto(Clinic clinic) =>
        new(clinic.Id, clinic.Name, clinic.Description, clinic.Phone, clinic.Address, clinic.Latitude, clinic.Longitude, clinic.IsVerified);

    private static ProductDto ToProductDto(Product product, string clinicName) =>
        new(product.Id, product.ClinicId, clinicName, product.Name, product.Description, product.Category, product.Price, product.Stock, product.ImageUrl, product.IsActive);

    private static OrderDto ToOrderDto(Order order, string clinicName) =>
        new(
            order.Id,
            order.ClinicId,
            clinicName,
            order.Status,
            order.Total,
            order.CreatedAtUtc,
            order.Items.Select(item => new OrderItemDto(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.Subtotal)).ToList());
}
