namespace ModuPOS.Inventory.Services;

using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Inventory.DTOs;
using ModuPOS.Inventory.Interfaces;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<GetProductsResponse>> GetProductsAsync(GetProductsRequest? request, CancellationToken cancellationToken = default)
    {
        if (request is null) return PagedResult<GetProductsResponse>.Failure("Request cannot be null");

        var query = _context.Products
            .AsNoTracking()
            .Where(p => !p.DeleteFlag);

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await (
            from p in query
            join c in _context.Categories.Where(x => !x.DeleteFlag) on p.CategoryId equals c.CategoryId into catGroup
            from cat in catGroup.DefaultIfEmpty()
            orderby p.ProductId
            select new GetProductsResponse
            {
                ProductId = p.ProductId,
                ProductCode = p.ProductCode,
                CategoryId = p.CategoryId,
                Name = p.Name,
                Price = p.Price,
                Quantity = p.Quantity,
                ImageUrl = p.ImageUrl,
                CreatedAt = p.CreatedAt,
                CategoryName = cat != null ? cat.CategoryName : null
            }
        )
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<GetProductsResponse>.Success(items, pagination);
    }

    public async Task<PagedResult<SearchProductsResponse>> SearchProductsAsync(SearchProductsRequest? request, CancellationToken cancellationToken = default)
    {
        if (request is null) return PagedResult<SearchProductsResponse>.Failure("Request cannot be null");

        var query = _context.Products
            .AsNoTracking()
            .Where(p => !p.DeleteFlag);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(p => p.Name.Contains(q) || p.ProductCode.Contains(q));
        }

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await (
            from p in query
            join c in _context.Categories.Where(x => !x.DeleteFlag) on p.CategoryId equals c.CategoryId into catGroup
            from cat in catGroup.DefaultIfEmpty()
            orderby p.ProductId
            select new SearchProductsResponse
            {
                ProductId = p.ProductId,
                ProductCode = p.ProductCode,
                CategoryId = p.CategoryId,
                Name = p.Name,
                Price = p.Price,
                Quantity = p.Quantity,
                ImageUrl = p.ImageUrl,
                CreatedAt = p.CreatedAt,
                CategoryName = cat != null ? cat.CategoryName : null
            }
        )
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<SearchProductsResponse>.Success(items, pagination);
    }

    public async Task<Result<ProductResponse>> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var p = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == id && !x.DeleteFlag, cancellationToken);

        if (p is null)
            return Result<ProductResponse>.Failure("Product not found.");

        string? categoryName = null;
        if (p.CategoryId.HasValue)
        {
            var cat = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == p.CategoryId.Value, cancellationToken);
            categoryName = cat?.CategoryName;
        }

        return Result<ProductResponse>.Success(new ProductResponse
        {
            ProductId = p.ProductId,
            ProductCode = p.ProductCode,
            CategoryId = p.CategoryId,
            Name = p.Name,
            Price = p.Price,
            Quantity = p.Quantity,
            ImageUrl = p.ImageUrl,
            CreatedAt = p.CreatedAt,
            CategoryName = categoryName
        });
    }

    public async Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductCode) || string.IsNullOrWhiteSpace(request.Name))
            return Result<ProductResponse>.Failure("Product code and name are required.");

        var exists = await _context.Products
            .AnyAsync(p => p.ProductCode == request.ProductCode && !p.DeleteFlag, cancellationToken);

        if (exists)
            return Result<ProductResponse>.Failure($"Product with code '{request.ProductCode}' already exists.");

        if (request.CategoryId.HasValue)
        {
            var catExists = await _context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId.Value && !c.DeleteFlag, cancellationToken);
            if (!catExists)
                return Result<ProductResponse>.Failure("Specified category does not exist.");
        }

        var entity = new Product
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

        _context.Products.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<ProductResponse>.Success(new ProductResponse
        {
            ProductId = entity.ProductId,
            ProductCode = entity.ProductCode,
            CategoryId = entity.CategoryId,
            Name = entity.Name,
            Price = entity.Price,
            Quantity = entity.Quantity,
            ImageUrl = entity.ImageUrl,
            CreatedAt = entity.CreatedAt
        });
    }

    public async Task<Result<ProductResponse>> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id && !p.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result<ProductResponse>.Failure("Product not found.");

        var codeInUse = await _context.Products
            .AnyAsync(p => p.ProductId != id && p.ProductCode == request.ProductCode && !p.DeleteFlag, cancellationToken);
        if (codeInUse)
            return Result<ProductResponse>.Failure($"Product with code '{request.ProductCode}' already exists.");

        if (request.CategoryId.HasValue)
        {
            var catExists = await _context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId.Value && !c.DeleteFlag, cancellationToken);
            if (!catExists)
                return Result<ProductResponse>.Failure("Specified category does not exist.");
        }

        entity.ProductCode = request.ProductCode;
        entity.CategoryId = request.CategoryId;
        entity.Name = request.Name;
        entity.Price = request.Price;
        entity.Quantity = request.Quantity;
        entity.ImageUrl = request.ImageUrl;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<ProductResponse>.Success(new ProductResponse
        {
            ProductId = entity.ProductId,
            ProductCode = entity.ProductCode,
            CategoryId = entity.CategoryId,
            Name = entity.Name,
            Price = entity.Price,
            Quantity = entity.Quantity,
            ImageUrl = entity.ImageUrl,
            CreatedAt = entity.CreatedAt
        });
    }

    public async Task<Result> DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id && !p.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result.Failure("Product not found.");

        entity.DeleteFlag = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success("Product deleted successfully.");
    }
}
