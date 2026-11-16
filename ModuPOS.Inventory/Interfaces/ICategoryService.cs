namespace ModuPOS.Inventory.Interfaces;

using ModuPOS.Inventory.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public interface ICategoryService
{
    Task<PagedResult<CategoryResponseDto>> GetCategoriesAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<Result<CategoryResponseDto>> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result<CategoryResponseDto>> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}
