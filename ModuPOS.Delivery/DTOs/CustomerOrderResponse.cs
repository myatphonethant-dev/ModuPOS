using ModuPOS.Delivery.Enums;

namespace ModuPOS.Delivery.DTOs;

public sealed class CustomerOrderResponse
{
    public string Id { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public int BranchId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public FulfillmentType FulfillmentType { get; init; }
    public DeliveryPaymentMethod PaymentMethod { get; init; }
    public OrderPaymentStatus PaymentStatus { get; init; }
    public CustomerOrderStatus Status { get; init; }
    public string? DeliveryAddress { get; init; }
    public string? DeliveryInstructions { get; init; }
    public string? CustomerNote { get; init; }
    public decimal Subtotal { get; init; }
    public decimal DeliveryFee { get; init; }
    public decimal Total { get; init; }
    public string? RiderName { get; init; }
    public string? RiderPhone { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<CustomerOrderItemResponse> Items { get; set; } = [];
}

public sealed class CustomerOrderItemResponse
{
    public string ProductCode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed class CreatedCustomerOrderResponse
{
    public CustomerOrderResponse Order { get; init; } = null!;
    public string? PickupCode { get; init; }
}