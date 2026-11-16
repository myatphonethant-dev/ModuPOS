namespace ModuPOS.Inventory.DTOs;

public record CreateProductRequest(
    string ProductCode,
    int? CategoryId,
    string Name,
    decimal Price,
    decimal Quantity,
    string? ImageUrl,
    string? CreatedBy = null);

public record UpdateProductRequest(
    string ProductCode,
    int? CategoryId,
    string Name,
    decimal Price,
    decimal Quantity,
    string? ImageUrl,
    string? UpdatedBy = null);

public record ProductResponseDto(
    int ProductId,
    string ProductCode,
    int? CategoryId,
    string Name,
    decimal Price,
    decimal Quantity,
    string? ImageUrl,
    DateTime CreatedAt,
    string? CategoryName = null);
