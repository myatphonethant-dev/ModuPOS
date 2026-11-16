namespace ModuPOS.Inventory.Services;

using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Inventory.DTOs;
using ModuPOS.Inventory.Interfaces;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public class CategoryService(AppDbContext context) : ICategoryService
{
    public async Task<PagedResult<CategoryResponseDto>> GetCategoriesAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Categories
            .AsNoTracking()
            .Where(c => !c.DeleteFlag);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.CategoryId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CategoryResponseDto(c.CategoryId, c.CategoryCode, c.CategoryName))
            .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<CategoryResponseDto>.Success(items, pagination);
    }

    public async Task<Result<CategoryResponseDto>> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CategoryId == id && !x.DeleteFlag, cancellationToken);

        if (c is null)
            return Result<CategoryResponseDto>.Failure("Category not found.");

        return Result<CategoryResponseDto>.Success(new CategoryResponseDto(c.CategoryId, c.CategoryCode, c.CategoryName));
    }

    public async Task<Result<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CategoryCode) || string.IsNullOrWhiteSpace(request.CategoryName))
            return Result<CategoryResponseDto>.Failure("Category code and name are required.");

        var exists = await context.Categories
            .AnyAsync(c => c.CategoryCode == request.CategoryCode && !c.DeleteFlag, cancellationToken);

        if (exists)
            return Result<CategoryResponseDto>.Failure($"Category with code '{request.CategoryCode}' already exists.");

        var entity = new Category
        {
            CategoryCode = request.CategoryCode,
            CategoryName = request.CategoryName,
            DeleteFlag = false
        };

        context.Categories.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CategoryResponseDto>.Success(new CategoryResponseDto(entity.CategoryId, entity.CategoryCode, entity.CategoryName));
    }

    public async Task<Result<CategoryResponseDto>> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await context.Categories.FirstOrDefaultAsync(c => c.CategoryId == id && !c.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result<CategoryResponseDto>.Failure("Category not found.");

        var codeInUse = await context.Categories
            .AnyAsync(c => c.CategoryId != id && c.CategoryCode == request.CategoryCode && !c.DeleteFlag, cancellationToken);
        if (codeInUse)
            return Result<CategoryResponseDto>.Failure($"Category with code '{request.CategoryCode}' already exists.");

        entity.CategoryCode = request.CategoryCode;
        entity.CategoryName = request.CategoryName;

        await context.SaveChangesAsync(cancellationToken);

        return Result<CategoryResponseDto>.Success(new CategoryResponseDto(entity.CategoryId, entity.CategoryCode, entity.CategoryName));
    }

    public async Task<Result> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Categories.FirstOrDefaultAsync(c => c.CategoryId == id && !c.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result.Failure("Category not found.");

        entity.DeleteFlag = true;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success("Category deleted successfully.");
    }
}
