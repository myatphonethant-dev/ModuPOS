using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Inventory.DTOs;
using ModuPOS.Shared.Results;

namespace ModuPOS.Inventory.Services.Category;

public interface ICategoryService
{
    Task<Result<List<CategoryResponseDto>>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Result<CategoryResponseDto>> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result<CategoryResponseDto>> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}

public class CategoryService(AppDbContext context) : ICategoryService
{
    public async Task<Result<List<CategoryResponseDto>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await context.Categories
            .AsNoTracking()
            .Where(c => !c.DeleteFlag)
            .Select(c => new CategoryResponseDto(c.CategoryId, c.CategoryCode, c.CategoryName))
            .ToListAsync(cancellationToken);

        return Result.Success(categories);
    }

    public async Task<Result<CategoryResponseDto>> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CategoryId == id && !x.DeleteFlag, cancellationToken);

        if (c is null)
            return Result.Failure<CategoryResponseDto>("Category not found.");

        return Result.Success(new CategoryResponseDto(c.CategoryId, c.CategoryCode, c.CategoryName));
    }

    public async Task<Result<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CategoryCode) || string.IsNullOrWhiteSpace(request.CategoryName))
            return Result.Failure<CategoryResponseDto>("Category code and name are required.");

        var exists = await context.Categories
            .AnyAsync(c => c.CategoryCode == request.CategoryCode && !c.DeleteFlag, cancellationToken);

        if (exists)
            return Result.Failure<CategoryResponseDto>($"Category with code '{request.CategoryCode}' already exists.");

        var entity = new ModuPOS.Database.AppDbContextModels.Category
        {
            CategoryCode = request.CategoryCode,
            CategoryName = request.CategoryName,
            DeleteFlag = false
        };

        context.Categories.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new CategoryResponseDto(entity.CategoryId, entity.CategoryCode, entity.CategoryName));
    }

    public async Task<Result<CategoryResponseDto>> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await context.Categories.FirstOrDefaultAsync(c => c.CategoryId == id && !c.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result.Failure<CategoryResponseDto>("Category not found.");

        var codeInUse = await context.Categories
            .AnyAsync(c => c.CategoryId != id && c.CategoryCode == request.CategoryCode && !c.DeleteFlag, cancellationToken);
        if (codeInUse)
            return Result.Failure<CategoryResponseDto>($"Category with code '{request.CategoryCode}' already exists.");

        entity.CategoryCode = request.CategoryCode;
        entity.CategoryName = request.CategoryName;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new CategoryResponseDto(entity.CategoryId, entity.CategoryCode, entity.CategoryName));
    }

    public async Task<Result> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Categories.FirstOrDefaultAsync(c => c.CategoryId == id && !c.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result.Failure("Category not found.");

        entity.DeleteFlag = true;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
