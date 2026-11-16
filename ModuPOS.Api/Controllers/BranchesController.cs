using Microsoft.AspNetCore.Mvc;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Services.Branch;
using ModuPOS.Shared.Pagination;

namespace ModuPOS.Api.Controllers;

/// <summary>
/// REST API controller for managing store branches.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BranchesController(IBranchService branchService) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of branches.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await branchService.GetBranchesAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single branch by identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await branchService.GetBranchByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Creates a new branch.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBranchRequest request, CancellationToken ct)
    {
        var result = await branchService.CreateBranchAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data!.BranchId }, result) : BadRequest(result);
    }

    /// <summary>
    /// Updates an existing branch.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBranchRequest request, CancellationToken ct)
    {
        var result = await branchService.UpdateBranchAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
