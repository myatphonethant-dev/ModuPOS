using Microsoft.AspNetCore.Mvc;
using ModuPOS.Inventory.DTOs;
using ModuPOS.Inventory.Services.Product;
using ModuPOS.Shared.Pagination;

namespace ModuPOS.Api.Controllers;

/// <summary>
/// REST API controller for managing inventory products.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of non-deleted products with optional filtering.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? categoryId, [FromQuery] string? search, [FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await productService.GetProductsAsync(categoryId, search, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single product by identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await productService.GetProductByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await productService.CreateProductAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data!.ProductId }, result) : BadRequest(result);
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await productService.UpdateProductAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Soft-deletes a product.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await productService.DeleteProductAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
