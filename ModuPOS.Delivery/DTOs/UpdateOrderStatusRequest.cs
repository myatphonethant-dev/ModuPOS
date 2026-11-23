using ModuPOS.Delivery.Enums;

namespace ModuPOS.Delivery.DTOs;

public sealed class UpdateOrderStatusRequest
{
    public CustomerOrderStatus Status { get; set; }
}
