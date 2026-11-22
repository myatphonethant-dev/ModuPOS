namespace ModuPOS.Auth.UnitTests;

using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Inventory.DTOs;
using ModuPOS.Inventory.Interfaces;
using ModuPOS.Inventory.Services;
using ModuPOS.Shared.Pagination;

public class ProductServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateProductAsync_ValidData_CreatesProductSuccessfully()
    {
        // Arrange
        await using var context = CreateContext();
        var service = new ProductService(context);
        var request = new CreateProductRequest
        {
            ProductCode = "P001",
            CategoryId = null,
            Name = "Coca Cola",
            Price = 1500m,
            Quantity = 50m,
            ImageUrl = null
        };

        // Act
        var result = await service.CreateProductAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("P001", result.Data.ProductCode);
        Assert.Equal("Coca Cola", result.Data.Name);
        Assert.Equal(1500m, result.Data.Price);

        var inDb = await context.Products.FirstOrDefaultAsync(p => p.ProductCode == "P001");
        Assert.NotNull(inDb);
    }

    [Fact]
    public async Task DeleteProductAsync_ExistingProduct_SetsDeleteFlagTrue()
    {
        // Arrange
        await using var context = CreateContext();
        var product = new Product
        {
            ProductId = 1,
            ProductCode = "P002",
            Name = "Pepsi",
            Price = 1400m,
            Quantity = 20m,
            DeleteFlag = false,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var service = new ProductService(context);

        // Act
        var result = await service.DeleteProductAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        var inDb = await context.Products.FindAsync(1);
        Assert.NotNull(inDb);
        Assert.True(inDb.DeleteFlag);
    }

    [Fact]
    public async Task GetProductsAsync_PaginationClamping_ReturnsPagedResult()
    {
        // Arrange
        await using var context = CreateContext();
        for (var i = 1; i <= 25; i++)
        {
            context.Products.Add(new Product
            {
                ProductId = i,
                ProductCode = $"P{i:D3}",
                Name = $"Product {i}",
                Price = 100m * i,
                Quantity = 10m,
                DeleteFlag = false,
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var service = new ProductService(context);
        var request = new GetProductsRequest { PageNumber = 2, PageSize = 10 };

        // Act
        var result = await service.GetProductsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(10, result.Data.Count);
        Assert.Equal(2, result.Pagination.PageNumber);
        Assert.Equal(10, result.Pagination.PageSize);
        Assert.Equal(25, result.Pagination.TotalCount);
        Assert.Equal(3, result.Pagination.TotalPages);
        Assert.True(result.Pagination.HasPreviousPage);
        Assert.True(result.Pagination.HasNextPage);
    }

    [Fact]
    public async Task GetProductsAsync_ExcludesSoftDeletedProducts()
    {
        // Arrange
        await using var context = CreateContext();
        context.Products.Add(new Product
        {
            ProductId = 101,
            ProductCode = "ACT01",
            Name = "Active Product",
            Price = 500m,
            Quantity = 5m,
            DeleteFlag = false,
            CreatedAt = DateTime.UtcNow
        });
        context.Products.Add(new Product
        {
            ProductId = 102,
            ProductCode = "DEL01",
            Name = "Deleted Product",
            Price = 500m,
            Quantity = 0m,
            DeleteFlag = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new ProductService(context);
        var request = new GetProductsRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetProductsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("ACT01", result.Data[0].ProductCode);
        Assert.DoesNotContain(result.Data, p => p.ProductCode == "DEL01");
    }

    [Fact]
    public async Task GetProductsAsync_WithCategoryIdFilter_ReturnsFilteredProducts()
    {
        // Arrange
        await using var context = CreateContext();
        context.Categories.Add(new Category { CategoryId = 1, CategoryCode = "CAT01", CategoryName = "Beverages", DeleteFlag = false });
        context.Categories.Add(new Category { CategoryId = 2, CategoryCode = "CAT02", CategoryName = "Snacks", DeleteFlag = false });
        context.Products.Add(new Product
        {
            ProductId = 1,
            ProductCode = "P001",
            Name = "Coke",
            CategoryId = 1,
            Price = 1000m,
            Quantity = 10m,
            DeleteFlag = false,
            CreatedAt = DateTime.UtcNow
        });
        context.Products.Add(new Product
        {
            ProductId = 2,
            ProductCode = "P002",
            Name = "Chips",
            CategoryId = 2,
            Price = 800m,
            Quantity = 15m,
            DeleteFlag = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new ProductService(context);
        var request = new GetProductsRequest { CategoryId = 1, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetProductsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("P001", result.Data[0].ProductCode);
        Assert.Equal("Beverages", result.Data[0].CategoryName);
    }

    [Fact]
    public async Task SearchProductsAsync_MatchingNameOrCode_ReturnsPagedResults()
    {
        // Arrange
        await using var context = CreateContext();
        context.Products.Add(new Product
        {
            ProductId = 1,
            ProductCode = "DRK001",
            Name = "Orange Juice",
            Price = 2000m,
            Quantity = 10m,
            DeleteFlag = false,
            CreatedAt = DateTime.UtcNow
        });
        context.Products.Add(new Product
        {
            ProductId = 2,
            ProductCode = "SNK002",
            Name = "Orange Candy",
            Price = 500m,
            Quantity = 25m,
            DeleteFlag = false,
            CreatedAt = DateTime.UtcNow
        });
        context.Products.Add(new Product
        {
            ProductId = 3,
            ProductCode = "OTH003",
            Name = "Mineral Water",
            Price = 500m,
            Quantity = 50m,
            DeleteFlag = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new ProductService(context);
        var request = new SearchProductsRequest { Query = "Orange", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.SearchProductsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.Pagination.TotalCount);
        Assert.Contains(result.Data, p => p.ProductCode == "DRK001");
        Assert.Contains(result.Data, p => p.ProductCode == "SNK002");
    }

    [Fact]
    public async Task SearchProductsAsync_NoMatch_ReturnsEmptyPagedResult()
    {
        // Arrange
        await using var context = CreateContext();
        context.Products.Add(new Product
        {
            ProductId = 1,
            ProductCode = "P001",
            Name = "Green Tea",
            Price = 1200m,
            Quantity = 10m,
            DeleteFlag = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new ProductService(context);
        var request = new SearchProductsRequest { Query = "Coffee", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.SearchProductsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Pagination.TotalCount);
    }

    [Fact]
    public async Task SearchProductsAsync_ExcludesSoftDeletedProducts()
    {
        // Arrange
        await using var context = CreateContext();
        context.Products.Add(new Product
        {
            ProductId = 1,
            ProductCode = "SRCH01",
            Name = "Active Match",
            Price = 1000m,
            Quantity = 5m,
            DeleteFlag = false,
            CreatedAt = DateTime.UtcNow
        });
        context.Products.Add(new Product
        {
            ProductId = 2,
            ProductCode = "SRCH02",
            Name = "Deleted Match",
            Price = 1000m,
            Quantity = 0m,
            DeleteFlag = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new ProductService(context);
        var request = new SearchProductsRequest { Query = "Match", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.SearchProductsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("SRCH01", result.Data[0].ProductCode);
        Assert.DoesNotContain(result.Data, p => p.ProductCode == "SRCH02");
    }
}
