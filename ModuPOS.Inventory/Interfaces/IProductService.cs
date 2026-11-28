namespace ModuPOS.Inventory.Interfaces;

using ModuPOS.Inventory.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public interface IProductService
{
    Task<PagedResult<GetProductsResponse>> GetProductsAsync(GetProductsRequest? request = null, CancellationToken cancellationToken = default);
    Task<PagedResult<SearchProductsResponse>> SearchProductsAsync(SearchProductsRequest? request = null, CancellationToken cancellationToken = default);
    Task<Result<ProductResponse>> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProductResponse>> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteProductAsync(int id, CancellationToken cancellationToken = default);
}
