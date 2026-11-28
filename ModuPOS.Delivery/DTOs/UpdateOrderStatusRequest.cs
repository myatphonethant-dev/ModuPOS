using ModuPOS.Delivery.Enums;

namespace ModuPOS.Delivery.DTOs;

public class UpdateOrderStatusRequest
{
    public CustomerOrderStatus Status { get; set; }
}