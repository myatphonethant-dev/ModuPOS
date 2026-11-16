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

public record GetProductsRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 10;

    public int? CategoryId { get; set; }

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
    }
}

public record GetProductsResponse(
    int ProductId,
    string ProductCode,
    int? CategoryId,
    string Name,
    decimal Price,
    decimal Quantity,
    string? ImageUrl,
    DateTime CreatedAt,
    string? CategoryName = null);

public record SearchProductsRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 10;

    public string? Query { get; set; }
    public int? CategoryId { get; set; }

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
    }
}

public record SearchProductsResponse(
    int ProductId,
    string ProductCode,
    int? CategoryId,
    string Name,
    decimal Price,
    decimal Quantity,
    string? ImageUrl,
    DateTime CreatedAt,
    string? CategoryName = null);
