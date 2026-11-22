using ModuPOS.Shared.Pagination;

namespace ModuPOS.Inventory.DTOs;

public class CreateProductRequest {

    public string ProductCode { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public string? CreatedBy { get; set; } = string.Empty;
}

public class UpdateProductRequest {
    public string ProductCode { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public string? UpdatedBy { get; set; } = string.Empty;
}

public sealed record ProductResponseDto
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

public class GetProductsRequest : PaginationRequest
{
    public int? CategoryId { get; set; }
}

public sealed record GetProductsResponse {
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

public class SearchProductsRequest : PaginationRequest
{
    public string? Query { get; set; }
    public int? CategoryId { get; set; }
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
