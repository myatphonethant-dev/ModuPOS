using System.ComponentModel.DataAnnotations;

namespace ModuPOS.Delivery.DTOs;

public sealed class VerifyPickupRequest
{
    [Required, StringLength(8, MinimumLength = 6)]
    public string PickupCode { get; set; } = string.Empty;
}
