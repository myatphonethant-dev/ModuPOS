using ModuPOS.Shared.Pagination;
using System.ComponentModel.DataAnnotations;

namespace ModuPOS.Inventory.DTOs;

public class CreateProductRequest
{
    [Required, MaxLength(50)]
    public required string ProductCode { get; set; }

    public int? CategoryId { get; set; }

    [Required, MaxLength(200)]
    public required string Name { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal Quantity { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdateProductRequest
{
    public string ProductCode { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal Quantity { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;
}

public class ProductResponse
{
    public int ProductId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal Quantity { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}


public class GetProductsRequest : PaginationRequest
{
    public int? CategoryId { get; set; }
}

public class GetProductsResponse
{
    public int ProductId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal Quantity { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}

public class SearchProductsRequest : PaginationRequest
{
    public string Query { get; set; } = string.Empty;

    public int? CategoryId { get; set; }
}

public class SearchProductsResponse
{
    public int ProductId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal Quantity { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}