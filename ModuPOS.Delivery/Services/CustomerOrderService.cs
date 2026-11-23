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
        if (request.Items.Count == 0)
            return Result<CreatedCustomerOrderResponse>.Failure("An order must contain at least one item.");

        if (request.FulfillmentType == FulfillmentType.Delivery &&
            string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            return Result<CreatedCustomerOrderResponse>.Failure("Delivery address is required.");
        }

        if (request.FulfillmentType == FulfillmentType.Pickup &&
            request.PaymentMethod == DeliveryPaymentMethod.CashOnDelivery)
        {
            return Result<CreatedCustomerOrderResponse>.Failure(
                "Cash on delivery is only available for delivery orders.");
        }

        var branchExists = await context.Branches
            .AsNoTracking()
            .AnyAsync(x => x.BranchId == request.BranchId, cancellationToken);

        if (!branchExists)
            return Result<CreatedCustomerOrderResponse>.Failure("Branch not found.");

        var productIds = request.Items.Select(x => x.ProductId).Distinct().ToArray();

        var products = await context.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.ProductId) && !x.DeleteFlag)
                .ToDictionaryAsync(x => x.ProductId, cancellationToken);

        if (products.Count != productIds.Length)
            return Result<CreatedCustomerOrderResponse>.Failure(
                "One or more products do not exist or are unavailable.");

        var orderId = Guid.NewGuid().ToString("N");
        var orderItems = new List<CustomerOrderItem>();

        foreach (var requestedItem in request.Items)
        {
            var product = products[requestedItem.ProductId];

            if (requestedItem.Quantity <= 0)
                return Result<CreatedCustomerOrderResponse>.Failure("Item quantity must be greater than zero.");

            orderItems.Add(new CustomerOrderItem
            {
                Id = Guid.NewGuid().ToString("N"),
                CustomerOrderId = orderId,
                ProductCode = product.ProductCode,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = requestedItem.Quantity,
                LineTotal = decimal.Round(product.Price * requestedItem.Quantity, 2)
            });
        }

        var subtotal = orderItems.Sum(x => x.LineTotal);
        var deliveryFee = request.FulfillmentType == FulfillmentType.Delivery
            ? request.DeliveryFee
            : 0m;

        var pickupCode = request.FulfillmentType == FulfillmentType.Pickup
            ? GeneratePickupCode()
            : null;

        var now = DateTime.UtcNow;
        var order = new CustomerOrder
        {
            Id = orderId,
            OrderNumber = $"DL{now:yyyyMMddHHmmss}{RandomNumberGenerator.GetInt32(1000, 9999)}",
            BranchId = request.BranchId,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            CustomerEmail = request.CustomerEmail?.Trim(),
            FulfillmentType = (int)request.FulfillmentType,
            PaymentMethod = (int)request.PaymentMethod,
            PaymentStatus = (int)OrderPaymentStatus.Pending,
            Status = (int)CustomerOrderStatus.Pending,
            DeliveryAddress = request.FulfillmentType == FulfillmentType.Delivery
                ? request.DeliveryAddress?.Trim()
                : null,
            DeliveryInstructions = request.DeliveryInstructions,
            CustomerNote = request.CustomerNote,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            Total = subtotal + deliveryFee,
            PickupCodeHash = pickupCode is null ? null : HashPickupCode(pickupCode),
            PickupCodeExpiresAt = pickupCode is null ? null : now.AddHours(24),
            CreatedAt = now
        };

        context.Set<CustomerOrder>().Add(order);
        context.Set<CustomerOrderItem>().AddRange(orderItems);

        await context.SaveChangesAsync(cancellationToken);

        return Result<CreatedCustomerOrderResponse>.Success(new CreatedCustomerOrderResponse
        {
            Order = Map(order),
            PickupCode = pickupCode
        }, "Order created successfully.");
    }

    public async Task<PagedResult<CustomerOrderResponse>> GetAllAsync(
    int? branchId,
    CustomerOrderStatus? status,
    PaginationRequest pagination,
    CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, pagination.PageNumber);
        var size = Math.Clamp(pagination.PageSize, 1, 100);

        var query = context.Set<CustomerOrder>()
            .AsNoTracking()
            .AsQueryable();

        if (branchId.HasValue)
            query = query.Where(x => x.BranchId == branchId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == (int)status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            return PagedResult<CustomerOrderResponse>.Success(
                new List<CustomerOrderResponse>(),
                new Pagination(page, size, totalCount));
        }

        var orderIds = orders.Select(x => x.Id).ToArray();

        var orderItems = await context.Set<CustomerOrderItem>()
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.CustomerOrderId))
            .ToListAsync(cancellationToken);

        var itemsLookup = orderItems.ToLookup(x => x.CustomerOrderId);

        var responses = orders.Select(order =>
        {
            var response = Map(order);

            response.Items = itemsLookup[order.Id]
                .Select(MapItem)
                .ToList();

            return response;
        }).ToList();

        return PagedResult<CustomerOrderResponse>.Success(
            responses,
            new Pagination(page, size, totalCount));
    }

    public async Task<Result<CustomerOrderResponse>> GetByIdAsync(
    string id,
    CancellationToken cancellationToken = default)
    {
        var order = await context.Set<CustomerOrder>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order is null)
            return Result<CustomerOrderResponse>.Failure("Order not found.");

        var items = await context.Set<CustomerOrderItem>()
            .AsNoTracking()
            .Where(x => x.CustomerOrderId == order.Id)
            .ToListAsync(cancellationToken);

        var response = Map(order);
        response.Items = items.Select(MapItem).ToList();

        return Result<CustomerOrderResponse>.Success(response);
    }

    public async Task<Result<CustomerOrderResponse>> UpdateStatusAsync(
    string id,
    CustomerOrderStatus status,
    CancellationToken cancellationToken = default)
    {
        var order = await context.Set<CustomerOrder>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order is null)
            return Result<CustomerOrderResponse>.Failure("Order not found.");

        if (!IsValidTransition(order, status))
            return Result<CustomerOrderResponse>.Failure(
                $"Cannot change order status from {order.Status} to {status}.");

        if (status == CustomerOrderStatus.Completed &&
            order.PaymentMethod == (int)DeliveryPaymentMethod.Card &&
            order.PaymentStatus != (int)OrderPaymentStatus.Paid)
        {
            return Result<CustomerOrderResponse>.Failure(
                "A card order must be paid before it can be completed.");
        }

        var now = DateTime.UtcNow;

        order.Status = (int)status;
        order.UpdatedAt = now;

        if (status == CustomerOrderStatus.OutForDelivery)
            order.DeliveryStartedAt = now;

        if (status == CustomerOrderStatus.Completed)
        {
            order.CompletedAt = now;

            if (order.FulfillmentType == (int)FulfillmentType.Delivery)
                order.DeliveredAt = now;

            if (order.FulfillmentType == (int)FulfillmentType.Pickup)
                order.PickedUpAt = now;

            if (order.PaymentMethod is
                (int)DeliveryPaymentMethod.CashOnDelivery or
                (int)DeliveryPaymentMethod.PayAtPickup)
            {
                order.PaymentStatus = (int)OrderPaymentStatus.Paid;
                order.PaidAt = now;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var response = Map(order);

        var items = await context.Set<CustomerOrderItem>()
            .AsNoTracking()
            .Where(x => x.CustomerOrderId == order.Id)
            .ToListAsync(cancellationToken);

        response.Items = items.Select(MapItem).ToList();

        return Result<CustomerOrderResponse>.Success(response);
    }

    public async Task<Result<CustomerOrderResponse>> AssignRiderAsync(
    string id,
    AssignRiderRequest request,
    CancellationToken cancellationToken = default)
    {
        var order = await context.Set<CustomerOrder>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order is null)
            return Result<CustomerOrderResponse>.Failure("Order not found.");

        if (order.FulfillmentType != (int)FulfillmentType.Delivery)
            return Result<CustomerOrderResponse>.Failure(
                "Riders can only be assigned to delivery orders.");

        if (order.Status is
            (int)CustomerOrderStatus.Completed or
            (int)CustomerOrderStatus.Cancelled or
            (int)CustomerOrderStatus.Rejected)
        {
            return Result<CustomerOrderResponse>.Failure(
                "Cannot assign a rider to a closed order.");
        }

        var now = DateTime.UtcNow;

        order.RiderName = request.RiderName.Trim();
        order.RiderPhone = request.RiderPhone.Trim();
        order.UpdatedAt = now;

        await context.SaveChangesAsync(cancellationToken);

        var response = Map(order);

        var items = await context.Set<CustomerOrderItem>()
            .AsNoTracking()
            .Where(x => x.CustomerOrderId == order.Id)
            .ToListAsync(cancellationToken);

        response.Items = items.Select(MapItem).ToList();

        return Result<CustomerOrderResponse>.Success(
            response,
            "Rider assigned successfully.");
    }

    public async Task<Result<CustomerOrderResponse>> VerifyPickupAsync(
    string id,
    string pickupCode,
    CancellationToken cancellationToken = default)
    {
        var order = await context.Set<CustomerOrder>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order is null)
            return Result<CustomerOrderResponse>.Failure("Order not found.");

        if (order.FulfillmentType != (int)FulfillmentType.Pickup)
            return Result<CustomerOrderResponse>.Failure("This order is not a pickup order.");

        if (order.Status != (int)CustomerOrderStatus.ReadyForPickup)
            return Result<CustomerOrderResponse>.Failure("Order is not ready for pickup.");

        if (order.PickupCodeExpiresAt is null ||
            order.PickupCodeExpiresAt <= DateTime.UtcNow ||
            string.IsNullOrWhiteSpace(order.PickupCodeHash))
        {
            return Result<CustomerOrderResponse>.Failure(
                "Pickup code has expired or is unavailable.");
        }

        if (string.IsNullOrWhiteSpace(pickupCode))
            return Result<CustomerOrderResponse>.Failure("Pickup code is required.");

        var suppliedHash = HashPickupCode(pickupCode);

        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(order.PickupCodeHash),
                Convert.FromHexString(suppliedHash)))
        {
            return Result<CustomerOrderResponse>.Failure("Invalid pickup code.");
        }

        var now = DateTime.UtcNow;

        order.Status = (int)CustomerOrderStatus.Completed;
        order.PaymentStatus = (int)OrderPaymentStatus.Paid;
        order.PickupCodeHash = null;
        order.PickupCodeExpiresAt = null;
        order.PickedUpAt = now;
        order.CompletedAt = now;
        order.PaidAt = now;
        order.UpdatedAt = now;

        await context.SaveChangesAsync(cancellationToken);

        var response = Map(order);

        var items = await context.Set<CustomerOrderItem>()
            .AsNoTracking()
            .Where(x => x.CustomerOrderId == order.Id)
            .ToListAsync(cancellationToken);

        response.Items = items.Select(MapItem).ToList();

        return Result<CustomerOrderResponse>.Success(
            response,
            "Pickup verified successfully.");
    }

    private static bool IsValidTransition(
    CustomerOrder order,
    CustomerOrderStatus next)
    {
        var currentStatus = (CustomerOrderStatus)order.Status;
        var fulfillmentType = (FulfillmentType)order.FulfillmentType;

        if (currentStatus is
            CustomerOrderStatus.Completed or
            CustomerOrderStatus.Cancelled or
            CustomerOrderStatus.Rejected)
        {
            return false;
        }

        if (next is CustomerOrderStatus.Cancelled or CustomerOrderStatus.Rejected)
            return currentStatus == CustomerOrderStatus.Pending;

        return fulfillmentType switch
        {
            FulfillmentType.Delivery => (currentStatus, next) switch
            {
                (CustomerOrderStatus.Pending, CustomerOrderStatus.Accepted) => true,

                (CustomerOrderStatus.Accepted, CustomerOrderStatus.Preparing) => true,

                (CustomerOrderStatus.Preparing, CustomerOrderStatus.OutForDelivery)
                    => !string.IsNullOrWhiteSpace(order.RiderName),

                (CustomerOrderStatus.OutForDelivery, CustomerOrderStatus.Completed) => true,

                _ => false
            },

            FulfillmentType.Pickup => (currentStatus, next) switch
            {
                (CustomerOrderStatus.Pending, CustomerOrderStatus.Accepted) => true,

                (CustomerOrderStatus.Accepted, CustomerOrderStatus.Preparing) => true,

                (CustomerOrderStatus.Preparing, CustomerOrderStatus.ReadyForPickup) => true,

                _ => false
            },

            _ => false
        };
    }

    private static string GeneratePickupCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string HashPickupCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    private static CustomerOrderResponse Map(CustomerOrder order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        BranchId = order.BranchId,
        CustomerName = order.CustomerName,
        CustomerPhone = order.CustomerPhone,
        CustomerEmail = order.CustomerEmail,

        FulfillmentType = (FulfillmentType)order.FulfillmentType,
        PaymentMethod = (DeliveryPaymentMethod)order.PaymentMethod,
        PaymentStatus = (OrderPaymentStatus)order.PaymentStatus,
        Status = (CustomerOrderStatus)order.Status,

        DeliveryAddress = order.DeliveryAddress,
        DeliveryInstructions = order.DeliveryInstructions,
        CustomerNote = order.CustomerNote,

        Subtotal = order.Subtotal,
        DeliveryFee = order.DeliveryFee,
        Total = order.Total,

        RiderName = order.RiderName,
        RiderPhone = order.RiderPhone,

        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt
    };

    private static CustomerOrderItemResponse MapItem(CustomerOrderItem item) => new()
    {
        ProductCode = item.ProductCode,
        ProductName = item.ProductName,
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity,
        LineTotal = item.LineTotal
    };
}