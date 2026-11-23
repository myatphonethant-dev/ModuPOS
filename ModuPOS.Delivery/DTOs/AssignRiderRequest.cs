using System.ComponentModel.DataAnnotations;

namespace ModuPOS.Delivery.DTOs;

public sealed class AssignRiderRequest
{
    [Required, MaxLength(200)]
    public string RiderName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string RiderPhone { get; set; } = string.Empty;
}
