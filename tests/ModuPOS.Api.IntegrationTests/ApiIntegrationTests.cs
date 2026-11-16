namespace ModuPOS.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Auth.DTOs;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Inventory.DTOs;
using ModuPOS.Shared.Pagination;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services
                    .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                                d.ServiceType == typeof(AppDbContext))
                    .ToList();

                foreach (var descriptor in toRemove)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                });
            });
        });
    }

    [Fact]
    public async Task GetCategories_ReturnsSuccessAndPagedEnvelope()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/categories?pageNumber=1&pageSize=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<CategoryResponseDto>>();
        Assert.NotNull(content);
        Assert.True(content.IsSuccess);
        Assert.NotNull(content.Pagination);
        Assert.Equal(1, content.Pagination.PageNumber);
        Assert.Equal(5, content.Pagination.PageSize);
    }

    [Fact]
    public async Task GetProducts_ReturnsSuccessAndPagedEnvelope()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/products?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<ProductResponseDto>>();
        Assert.NotNull(content);
        Assert.True(content.IsSuccess);
        Assert.NotNull(content.Pagination);
    }

    [Fact]
    public async Task GetBranches_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/branches");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("non_existent_user", "wrong_password"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
