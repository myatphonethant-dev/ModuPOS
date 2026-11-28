using ModuPOS.Shared.Pagination;
using System.ComponentModel.DataAnnotations;

namespace ModuPOS.Inventory.DTOs;

public sealed record CreateProductRequest
{
    [Required, MaxLength(50)]
    public required string ProductCode { get; init; }

    public int? CategoryId { get; init; }

    [Required, MaxLength(200)]
    public required string Name { get; init; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal Price { get; init; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal Quantity { get; init; }

    public string? ImageUrl { get; init; }
}

public class UpdateProductRequest
{
    public string ProductCode { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal Quantity { get; set; }

    public string? ImageUrl { get; set; }

    public string? UpdatedBy { get; set; } = string.Empty;
}

public sealed record ProductResponse
{
    public int ProductId { get; init; }

    public string ProductCode { get; init; } = string.Empty;

    public int? CategoryId { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public decimal Quantity { get; init; }

    public string? ImageUrl { get; init; }

    public DateTime CreatedAt { get; init; }

    public string? CategoryName { get; init; }
}


public sealed record GetProductsRequest : PaginationRequest
{
    public int? CategoryId { get; init; }
}

public sealed record GetProductsResponse
{
    public int ProductId { get; init; }

    public string ProductCode { get; init; } = string.Empty;

    public int? CategoryId { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public decimal Quantity { get; init; }

    public string? ImageUrl { get; init; }

    public DateTime CreatedAt { get; init; }

    public string? CategoryName { get; init; } = string.Empty;
}

public sealed record SearchProductsRequest : PaginationRequest
{
    public string? Query { get; init; }

    public int? CategoryId { get; init; }
}

public sealed record SearchProductsResponse
{
    public int ProductId { get; init; }

    public string ProductCode { get; init; } = string.Empty;

    public int? CategoryId { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public decimal Quantity { get; init; }

    public string? ImageUrl { get; init; }

    public DateTime CreatedAt { get; init; }

    public string? CategoryName { get; init; } = string.Empty;
}