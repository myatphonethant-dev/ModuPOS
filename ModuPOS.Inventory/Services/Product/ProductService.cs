using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Inventory.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

namespace ModuPOS.Inventory.Services.Product;

/// <summary>
/// Defines business operations for managing products and catalog inventory.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Retrieves a paginated list of non-deleted products with optional filtering.
    /// </summary>
    /// <param name="categoryId">Optional category identifier filter.</param>
    /// <param name="search">Optional name or product code keyword.</param>
    /// <param name="request">The pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of product response DTOs.</returns>
    Task<PagedResult<ProductResponseDto>> GetProductsAsync(int? categoryId = null, string? search = null, PaginationRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a product by its identifier.
    /// </summary>
    /// <param name="id">The product primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The product response DTO if found; otherwise failure.</returns>
    Task<Result<ProductResponseDto>> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="request">The product creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created product response DTO if successful; otherwise failure.</returns>
    Task<Result<ProductResponseDto>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">The product primary key.</param>
    /// <param name="request">The product update details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated product response DTO if successful; otherwise failure.</returns>
    Task<Result<ProductResponseDto>> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a product by setting its DeleteFlag to true.
    /// </summary>
    /// <param name="id">The product primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> DeleteProductAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides EF Core implementation for <see cref="IProductService"/>.
/// </summary>
public class ProductService(AppDbContext context) : IProductService
{
    /// <inheritdoc />
    public async Task<PagedResult<ProductResponseDto>> GetProductsAsync(int? categoryId = null, string? search = null, PaginationRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new PaginationRequest();

        var query = context.Products
            .AsNoTracking()
            .Where(p => !p.DeleteFlag);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p => p.Name.Contains(s) || p.ProductCode.Contains(s));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await (
            from p in query
            join c in context.Categories.Where(x => !x.DeleteFlag) on p.CategoryId equals c.CategoryId into catGroup
            from cat in catGroup.DefaultIfEmpty()
            orderby p.ProductId
            select new ProductResponseDto(
                p.ProductId,
                p.ProductCode,
                p.CategoryId,
                p.Name,
                p.Price,
                p.Quantity,
                p.ImageUrl,
                p.CreatedAt,
                cat != null ? cat.CategoryName : null)
        )
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<ProductResponseDto>.Success(items, pagination);
    }

    /// <inheritdoc />
    public async Task<Result<ProductResponseDto>> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var p = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == id && !x.DeleteFlag, cancellationToken);

        if (p is null)
            return Result<ProductResponseDto>.Failure("Product not found.");

        string? categoryName = null;
        if (p.CategoryId.HasValue)
        {
            var cat = await context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == p.CategoryId.Value, cancellationToken);
            categoryName = cat?.CategoryName;
        }

        return Result<ProductResponseDto>.Success(new ProductResponseDto(
            p.ProductId, p.ProductCode, p.CategoryId, p.Name, p.Price, p.Quantity, p.ImageUrl, p.CreatedAt, categoryName));
    }

    /// <inheritdoc />
    public async Task<Result<ProductResponseDto>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductCode) || string.IsNullOrWhiteSpace(request.Name))
            return Result<ProductResponseDto>.Failure("Product code and name are required.");

        var exists = await context.Products
            .AnyAsync(p => p.ProductCode == request.ProductCode && !p.DeleteFlag, cancellationToken);

        if (exists)
            return Result<ProductResponseDto>.Failure($"Product with code '{request.ProductCode}' already exists.");

        if (request.CategoryId.HasValue)
        {
            var catExists = await context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId.Value && !c.DeleteFlag, cancellationToken);
            if (!catExists)
                return Result<ProductResponseDto>.Failure("Specified category does not exist.");
        }

        var entity = new ModuPOS.Database.AppDbContextModels.Product
        {
            ProductCode = request.ProductCode,
            CategoryId = request.CategoryId,
            Name = request.Name,
            Price = request.Price,
            Quantity = request.Quantity,
            ImageUrl = request.ImageUrl,
            DeleteFlag = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        context.Products.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result<ProductResponseDto>.Success(new ProductResponseDto(
            entity.ProductId, entity.ProductCode, entity.CategoryId, entity.Name, entity.Price, entity.Quantity, entity.ImageUrl, entity.CreatedAt));
    }

    /// <inheritdoc />
    public async Task<Result<ProductResponseDto>> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await context.Products.FirstOrDefaultAsync(p => p.ProductId == id && !p.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result<ProductResponseDto>.Failure("Product not found.");

        var codeInUse = await context.Products
            .AnyAsync(p => p.ProductId != id && p.ProductCode == request.ProductCode && !p.DeleteFlag, cancellationToken);
        if (codeInUse)
            return Result<ProductResponseDto>.Failure($"Product with code '{request.ProductCode}' already exists.");

        if (request.CategoryId.HasValue)
        {
            var catExists = await context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId.Value && !c.DeleteFlag, cancellationToken);
            if (!catExists)
                return Result<ProductResponseDto>.Failure("Specified category does not exist.");
        }

        entity.ProductCode = request.ProductCode;
        entity.CategoryId = request.CategoryId;
        entity.Name = request.Name;
        entity.Price = request.Price;
        entity.Quantity = request.Quantity;
        entity.ImageUrl = request.ImageUrl;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy;

        await context.SaveChangesAsync(cancellationToken);

        return Result<ProductResponseDto>.Success(new ProductResponseDto(
            entity.ProductId, entity.ProductCode, entity.CategoryId, entity.Name, entity.Price, entity.Quantity, entity.ImageUrl, entity.CreatedAt));
    }

    /// <inheritdoc />
    public async Task<Result> DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Products.FirstOrDefaultAsync(p => p.ProductId == id && !p.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result.Failure("Product not found.");

        entity.DeleteFlag = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success("Product deleted successfully.");
    }
}
