namespace ModuPOS.Inventory.Interfaces;

using ModuPOS.Inventory.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public interface IProductService
{
    Task<PagedResult<ProductResponseDto>> GetProductsAsync(int? categoryId = null, string? search = null, PaginationRequest? request = null, CancellationToken cancellationToken = default);
    Task<Result<ProductResponseDto>> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ProductResponseDto>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProductResponseDto>> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteProductAsync(int id, CancellationToken cancellationToken = default);
}
