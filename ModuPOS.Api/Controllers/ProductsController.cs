namespace ModuPOS.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using ModuPOS.Inventory.DTOs;
using ModuPOS.Inventory.Interfaces;
using ModuPOS.Shared.Pagination;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetProductsRequest request, CancellationToken ct)
    {
        var result = await productService.GetProductsAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] SearchProductsRequest request, CancellationToken ct)
    {
        var result = await productService.SearchProductsAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await productService.GetProductByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await productService.CreateProductAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data!.ProductId }, result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await productService.UpdateProductAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await productService.DeleteProductAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
