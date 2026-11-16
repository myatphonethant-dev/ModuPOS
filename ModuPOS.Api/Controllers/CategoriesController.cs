using Microsoft.AspNetCore.Mvc;
using ModuPOS.Inventory.DTOs;
using ModuPOS.Inventory.Services.Category;
using ModuPOS.Shared.Pagination;

namespace ModuPOS.Api.Controllers;

/// <summary>
/// REST API controller for managing inventory categories.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of non-deleted categories.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await categoryService.GetCategoriesAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single category by identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await categoryService.GetCategoryByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await categoryService.CreateCategoryAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data!.CategoryId }, result) : BadRequest(result);
    }

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        var result = await categoryService.UpdateCategoryAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Soft-deletes a category.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await categoryService.DeleteCategoryAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
