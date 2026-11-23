using System;
using System.Collections.Generic;

namespace ModuPOS.Database.AppDbContextModels;

public partial class CustomerOrder
{
    public string Id { get; set; } = null!;

    public string OrderNumber { get; set; } = null!;

    public int BranchId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string CustomerPhone { get; set; } = null!;

    public string? CustomerEmail { get; set; }

    public int FulfillmentType { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? DeliveryInstructions { get; set; }

    public string? CustomerNote { get; set; }

    public int Status { get; set; }

    public int PaymentMethod { get; set; }

    public int PaymentStatus { get; set; }

    public string? PaymentReference { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? RiderName { get; set; }

    public string? RiderPhone { get; set; }

    public DateTime? DeliveryStartedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public string? PickupCodeHash { get; set; }

    public DateTime? PickupCodeExpiresAt { get; set; }

    public DateTime? PickedUpAt { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DeliveryFee { get; set; }

    public decimal Total { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
