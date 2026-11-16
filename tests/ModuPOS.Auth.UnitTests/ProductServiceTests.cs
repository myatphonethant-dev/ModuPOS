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
        var request = new CreateProductRequest("P001", null, "Coca Cola", 1500m, 50m, null);

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
        var request = new PaginationRequest { PageNumber = 2, PageSize = 10 };

        // Act
        var result = await service.GetProductsAsync(request: request);

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
        var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetProductsAsync(request: request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("ACT01", result.Data[0].ProductCode);
        Assert.DoesNotContain(result.Data, p => p.ProductCode == "DEL01");
    }
}
