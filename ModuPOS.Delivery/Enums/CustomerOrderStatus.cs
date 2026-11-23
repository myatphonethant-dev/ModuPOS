namespace ModuPOS.Delivery.Enums;

public enum CustomerOrderStatus
{
    Pending = 1,
    Accepted = 2,
    Preparing = 3,
    ReadyForPickup = 4,
    OutForDelivery = 5,
    Completed = 6,
    Cancelled = 7,
    Rejected = 8
}