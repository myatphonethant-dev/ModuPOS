using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Auth.DTOs;
using ModuPOS.Database.AppDbContextModels;

namespace ModuPOS.Api.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all DbContext related registrations
                var toRemove = services
                    .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                                d.ServiceType == typeof(AppDbContext))
                    .ToList();

                foreach (var descriptor in toRemove)
                {
                    services.Remove(descriptor);
                }

                // Add In-Memory DbContext
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                });
            });
        });
    }

    [Fact]
    public async Task GetCategories_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBranches_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/branches");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("non_existent_user", "wrong_password"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
