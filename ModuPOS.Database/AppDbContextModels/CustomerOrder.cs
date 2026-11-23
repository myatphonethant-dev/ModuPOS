using System;
using System.Collections.Generic;

namespace ModuPOS.Database.AppDbContextModels;

public partial class CustomerOrder
{
    public string Id { get; set; } = null!;

    public string OrderNumber { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string CustomerPhone { get; set; } = null!;

    public string? CustomerEmail { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? DeliveryInstructions { get; set; }

    public string? CustomerNote { get; set; }

    public string? RiderName { get; set; }

    public string? RiderPhone { get; set; }

    public string? PickupCodeHash { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DeliveryFee { get; set; }

    public decimal Total { get; set; }

    public int BranchId { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
