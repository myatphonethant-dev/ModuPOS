using System.ComponentModel.DataAnnotations;
using ModuPOS.Delivery.Enums;

namespace ModuPOS.Delivery.DTOs;

public class CreateCustomerOrderRequest
{
    [Range(1, int.MaxValue)]
    public int BranchId { get; set; }

    [Required, MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string CustomerPhone { get; set; } = string.Empty;

    [EmailAddress, MaxLength(200)]
    public string CustomerEmail { get; set; } = string.Empty;

    public FulfillmentType FulfillmentType { get; set; }
    public DeliveryPaymentMethod PaymentMethod { get; set; }

    [MaxLength(1000)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string DeliveryInstructions { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string CustomerNote { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal DeliveryFee { get; set; }

    [Required, MinLength(1)]
    public List<CreateCustomerOrderItemRequest> Items { get; set; } = [];
}

public class CreateCustomerOrderItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(typeof(decimal), "0.001", "999999")]
    public decimal Quantity { get; set; }
}