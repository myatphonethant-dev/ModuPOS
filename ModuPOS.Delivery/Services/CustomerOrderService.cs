using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Delivery.DTOs;
using ModuPOS.Delivery.Enums;
using ModuPOS.Delivery.Interfaces;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

namespace ModuPOS.Delivery.Services;

public sealed class CustomerOrderService(AppDbContext context) : ICustomerOrderService
{
    public async Task<Result<CreatedCustomerOrderResponse>> CreateAsync(
        CreateCustomerOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        //    if (request.Items.Count == 0)
        //        return Result<CreatedCustomerOrderResponse>.Failure("An order must contain at least one item.");

        //    if (request.FulfillmentType == FulfillmentType.Delivery &&
        //        string.IsNullOrWhiteSpace(request.DeliveryAddress))
        //    {
        //        return Result<CreatedCustomerOrderResponse>.Failure("Delivery address is required.");
        //    }

        //    if (request.FulfillmentType == FulfillmentType.Pickup &&
        //        request.PaymentMethod == DeliveryPaymentMethod.CashOnDelivery)
        //    {
        //        return Result<CreatedCustomerOrderResponse>.Failure(
        //            "Cash on delivery is only available for delivery orders.");
        //    }

        //    var branchExists = await context.Branches
        //        .AsNoTracking()
        //        .AnyAsync(x => x.BranchId == request.BranchId, cancellationToken);

        //    if (!branchExists)
        //        return Result<CreatedCustomerOrderResponse>.Failure("Branch not found.");

        //    var productIds = request.Items.Select(x => x.ProductId).Distinct().ToArray();

        //    var products = await context.Products
        //        .AsNoTracking()
        //        .Where(x => productIds.Contains(x.ProductId) && !x.DeleteFlag)
        //        .ToDictionaryAsync(x => x.ProductId, cancellationToken);

        //    if (products.Count != productIds.Length)
        //        return Result<CreatedCustomerOrderResponse>.Failure(
        //            "One or more products do not exist or are unavailable.");

        //    var orderItems = new List<CustomerOrderItem>();

        //    foreach (var requestedItem in request.Items)
        //    {
        //        var product = products[requestedItem.ProductId];

        //        if (requestedItem.Quantity <= 0)
        //            return Result<CreatedCustomerOrderResponse>.Failure("Item quantity must be greater than zero.");

        //        orderItems.Add(new CustomerOrderItem
        //        {
        //            Id = "",
        //            ProductCode = product.ProductCode,
        //            ProductName = product.Name,
        //            UnitPrice = product.Price,
        //            Quantity = requestedItem.Quantity,
        //            LineTotal = decimal.Round(product.Price * requestedItem.Quantity, 2)
        //        });
        //    }

        //    var subtotal = orderItems.Sum(x => x.LineTotal);
        //    var deliveryFee = request.FulfillmentType == FulfillmentType.Delivery
        //        ? request.DeliveryFee
        //        : 0m;

        //    var pickupCode = request.FulfillmentType == FulfillmentType.Pickup
        //        ? GeneratePickupCode()
        //        : null;

        //    var now = DateTime.UtcNow;
        //    var order = new CustomerOrder
        //    {
        //        Id = "",
        //        OrderNumber = $"DL{now:yyyyMMddHHmmss}{RandomNumberGenerator.GetInt32(1000, 9999)}",
        //        BranchId = request.BranchId,
        //        CustomerName = request.CustomerName.Trim(),
        //        CustomerPhone = request.CustomerPhone.Trim(),
        //        CustomerEmail = request.CustomerEmail?.Trim(),
        //        FulfillmentType = request.FulfillmentType,
        //        PaymentMethod = request.PaymentMethod,
        //        PaymentStatus = OrderPaymentStatus.Pending,
        //        Status = (int)CustomerOrderStatus.Pending,
        //        DeliveryAddress = request.FulfillmentType == FulfillmentType.Delivery
        //            ? request.DeliveryAddress?.Trim()
        //            : null,
        //        DeliveryInstructions = request.DeliveryInstructions,
        //        CustomerNote = request.CustomerNote,
        //        Subtotal = subtotal,
        //        DeliveryFee = deliveryFee,
        //        Total = subtotal + deliveryFee,
        //        PickupCodeHash = pickupCode is null ? null : HashPickupCode(pickupCode),
        //        PickupCodeExpiresAt = pickupCode is null ? null : now.AddHours(24),
        //        CreatedAt = now,
        //        Items = orderItems
        //    };

        //    context.Set<CustomerOrder>().Add(order);
        //    await context.SaveChangesAsync(cancellationToken);

        //return Result<CreatedCustomerOrderResponse>.Success(new CreatedCustomerOrderResponse
        //{
        //        Order = Map(order),
        //        PickupCode = pickupCode
        //}, "Order created successfully.");
#warning need to fix this method, currently disabled for testing purposes
        return Result<CreatedCustomerOrderResponse>.Failure("Order creation is currently disabled.");
    }

    public async Task<PagedResult<CustomerOrderResponse>> GetAllAsync(
        int? branchId,
        CustomerOrderStatus? status,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        //    var page = Math.Max(1, pagination.PageNumber);
        //    var size = Math.Clamp(pagination.PageSize, 1, 100);

        //    var query = context.Set<CustomerOrder>()
        //        .AsNoTracking()
        //        .Include(x => x.Items)
        //        .AsQueryable();

        //    if (branchId.HasValue)
        //        query = query.Where(x => x.BranchId == branchId.Value);

        //    if (status.HasValue)
        //        query = query.Where(x => x.Status == status.Value);

        //    var totalCount = await query.CountAsync(cancellationToken);
        //    var orders = await query
        //        .OrderByDescending(x => x.CreatedAt)
        //        .Skip((page - 1) * size)
        //        .Take(size)
        //        .ToListAsync(cancellationToken);

        //return PagedResult<CustomerOrderResponse>.Success(
        //orders.Select(Map).ToList(),
        //new Pagination(page, size, totalCount)
        //);
#warning need to fix this method, currently disabled for testing purposes
        return PagedResult<CustomerOrderResponse>.Success(
            new List<CustomerOrderResponse>(),
            new Pagination(1, 10, 0)
        );
    }

    public async Task<Result<CustomerOrderResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        //    var order = await context.Set<CustomerOrder>()
        //        .AsNoTracking()
        //        .Include(x => x.Items)
        //        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        //return order is null
        //    ? Result<CustomerOrderResponse>.Failure("Order not found.")
        //    : Result<CustomerOrderResponse>.Success(Map(order));
#warning need to fix this method, currently disabled for testing purposes
        return Result<CustomerOrderResponse>.Failure("Get order by ID is currently disabled.");
    }

    public async Task<Result<CustomerOrderResponse>> UpdateStatusAsync(
        Guid id,
        CustomerOrderStatus status,
        CancellationToken cancellationToken = default)
    {
        //    var order = await context.Set<CustomerOrder>()
        //        .Include(x => x.Items)
        //        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        //    if (order is null)
        //        return Result<CustomerOrderResponse>.Failure("Order not found.");

        //    if (!IsValidTransition(order, status))
        //        return Result<CustomerOrderResponse>.Failure(
        //            $"Cannot change order status from {order.Status} to {status}.");

        //    if (status == CustomerOrderStatus.Completed &&
        //        order.PaymentMethod == DeliveryPaymentMethod.Card &&
        //        order.PaymentStatus != OrderPaymentStatus.Paid)
        //    {
        //        return Result<CustomerOrderResponse>.Failure(
        //            "A card order must be paid before it can be completed.");
        //    }

        //    order.Status = status;
        //    order.UpdatedAt = DateTime.UtcNow;

        //    if (status == CustomerOrderStatus.OutForDelivery)
        //        order.PickedUpAt = DateTime.UtcNow;

        //    if (status == CustomerOrderStatus.Completed)
        //    {
        //        order.DeliveredAt = DateTime.UtcNow;

        //        if (order.PaymentMethod is DeliveryPaymentMethod.CashOnDelivery
        //            or DeliveryPaymentMethod.PayAtPickup)
        //        {
        //            order.PaymentStatus = OrderPaymentStatus.Paid;
        //        }
        //    }

        //    await context.SaveChangesAsync(cancellationToken);
        //    return Result<CustomerOrderResponse>.Success(Map(order));
#warning need to fix this method, currently disabled for testing purposes
        return Result<CustomerOrderResponse>.Failure("Order status update is currently disabled.");
    }

    public async Task<Result<CustomerOrderResponse>> AssignRiderAsync(
        Guid id,
        AssignRiderRequest request,
        CancellationToken cancellationToken = default)
    {
        //    var order = await context.Set<CustomerOrder>()
        //        .Include(x => x.Items)
        //        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        //    if (order is null)
        //        return Result<CustomerOrderResponse>.Failure("Order not found.");

        //    if (order.FulfillmentType != FulfillmentType.Delivery)
        //        return Result<CustomerOrderResponse>.Failure("Riders can only be assigned to delivery orders.");

        //    if (order.Status is CustomerOrderStatus.Completed
        //        or CustomerOrderStatus.Cancelled
        //        or CustomerOrderStatus.Rejected)
        //    {
        //        return Result<CustomerOrderResponse>.Failure("Cannot assign a rider to a closed order.");
        //    }

        //    order.RiderName = request.RiderName.Trim();
        //    order.RiderPhone = request.RiderPhone.Trim();
        //    order.RiderAssignedAt = DateTime.UtcNow;
        //    order.UpdatedAt = DateTime.UtcNow;

        //    await context.SaveChangesAsync(cancellationToken);
        //return Result<CustomerOrderResponse>.Success(Map(order), "Rider assigned successfully.");
#warning need to fix this method, currently disabled for testing purposes
        return Result<CustomerOrderResponse>.Failure("Rider assignment is currently disabled.");
    }

    public async Task<Result<CustomerOrderResponse>> VerifyPickupAsync(
        Guid id,
        string pickupCode,
        CancellationToken cancellationToken = default)
    {
        //    var order = await context.Set<CustomerOrder>()
        //        .Include(x => x.Items)
        //        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        //    if (order is null)
        //        return Result<CustomerOrderResponse>.Failure("Order not found.");

        //    if (order.FulfillmentType != FulfillmentType.Pickup)
        //        return Result<CustomerOrderResponse>.Failure("This order is not a pickup order.");

        //    if (order.Status != CustomerOrderStatus.ReadyForPickup)
        //        return Result<CustomerOrderResponse>.Failure("Order is not ready for pickup.");

        //    if (order.PickupCodeExpiresAt is null ||
        //        order.PickupCodeExpiresAt <= DateTime.UtcNow ||
        //        string.IsNullOrWhiteSpace(order.PickupCodeHash))
        //    {
        //        return Result<CustomerOrderResponse>.Failure("Pickup code has expired or is unavailable.");
        //    }

        //    var suppliedHash = HashPickupCode(pickupCode);
        //    if (!CryptographicOperations.FixedTimeEquals(
        //            Convert.FromHexString(order.PickupCodeHash),
        //            Convert.FromHexString(suppliedHash)))
        //    {
        //        return Result<CustomerOrderResponse>.Failure("Invalid pickup code.");
        //    }

        //    order.Status = CustomerOrderStatus.Completed;
        //    order.PaymentStatus = OrderPaymentStatus.Paid;
        //    order.PickupCodeHash = null; // One-time verification.
        //    order.PickupCodeExpiresAt = null;
        //    order.DeliveredAt = DateTime.UtcNow;
        //    order.UpdatedAt = DateTime.UtcNow;

        //    await context.SaveChangesAsync(cancellationToken);
        //    return Result<CustomerOrderResponse>.Success(Map(order), "Pickup verified successfully.");
# warning need to fix this method, currently disabled for testing purposes
        return Result<CustomerOrderResponse>.Failure("Pickup verification is currently disabled.");
    }

    private static bool IsValidTransition(CustomerOrder order, CustomerOrderStatus next)
    {
        //    if (order.Status is CustomerOrderStatus.Completed
        //        or CustomerOrderStatus.Cancelled
        //        or CustomerOrderStatus.Rejected)
        //        return false;

        //    if (next is CustomerOrderStatus.Cancelled or CustomerOrderStatus.Rejected)
        //        return order.Status == CustomerOrderStatus.Pending;

        //return order.FulfillmentType switch
        //{
        //FulfillmentType.Delivery => (order.Status, next) switch
        //{
        //    (CustomerOrderStatus.Pending, CustomerOrderStatus.Accepted) => true,
        //    (CustomerOrderStatus.Accepted, CustomerOrderStatus.Preparing) => true,
        //    (CustomerOrderStatus.Preparing, CustomerOrderStatus.OutForDelivery)
        //        => !string.IsNullOrWhiteSpace(order.RiderName),
        //    (CustomerOrderStatus.OutForDelivery, CustomerOrderStatus.Completed) => true,
        //    _ => false
        //},
        //FulfillmentType.Pickup => (order.Status, next) switch
        //{
        //    (CustomerOrderStatus.Pending, CustomerOrderStatus.Accepted) => true,
        //    (CustomerOrderStatus.Accepted, CustomerOrderStatus.Preparing) => true,
        //    (CustomerOrderStatus.Preparing, CustomerOrderStatus.ReadyForPickup) => true,
        //    _ => false
        //},
        //_ => false
        //};
        return true;
    }

    private static string GeneratePickupCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string HashPickupCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    private static CustomerOrderResponse Map(CustomerOrder order) => new()
    {
        //    Id = order.Id,
        //    OrderNumber = order.OrderNumber,
        //    BranchId = order.BranchId,
        //    CustomerName = order.CustomerName,
        //    CustomerPhone = order.CustomerPhone,
        //    CustomerEmail = order.CustomerEmail,
        //    FulfillmentType = order.FulfillmentType,
        //    PaymentMethod = order.PaymentMethod,
        //    PaymentStatus = order.PaymentStatus,
        //    Status = order.Status,
        //    DeliveryAddress = order.DeliveryAddress,
        //    DeliveryInstructions = order.DeliveryInstructions,
        //    CustomerNote = order.CustomerNote,
        //    Subtotal = order.Subtotal,
        //    DeliveryFee = order.DeliveryFee,
        //    Total = order.Total,
        //    RiderName = order.RiderName,
        //    RiderPhone = order.RiderPhone,
        //    CreatedAt = order.CreatedAt,
        //    UpdatedAt = order.UpdatedAt,
        //    Items = order.Items.Select(item => new CustomerOrderItemResponse
        //    {
        //        ProductId = item.ProductId,
        //        ProductCode = item.ProductCode,
        //        ProductName = item.ProductName,
        //        UnitPrice = item.UnitPrice,
        //        Quantity = item.Quantity,
        //        LineTotal = item.LineTotal
        //    }).ToList()
    };
}