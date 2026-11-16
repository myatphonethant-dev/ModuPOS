using Microsoft.AspNetCore.Mvc;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Services.Shift;
using ModuPOS.Shared.Pagination;

namespace ModuPOS.Api.Controllers;

/// <summary>
/// REST API controller for managing work shifts and staff shift assignments.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ShiftsController(IShiftService shiftService) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of operational shifts.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await shiftService.GetShiftsAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single shift by identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await shiftService.GetShiftByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Creates a new operational shift.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShiftRequest request, CancellationToken ct)
    {
        var result = await shiftService.CreateShiftAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data!.ShiftId }, result) : BadRequest(result);
    }

    /// <summary>
    /// Updates an existing operational shift.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShiftRequest request, CancellationToken ct)
    {
        var result = await shiftService.UpdateShiftAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Assigns a staff member to an operational shift.
    /// </summary>
    [HttpPost("assign")]
    public async Task<IActionResult> AssignStaffShift([FromBody] AssignStaffShiftRequest request, CancellationToken ct)
    {
        var result = await shiftService.AssignStaffShiftAsync(request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Retrieves a paginated list of staff shift assignments with an optional shift filter.
    /// </summary>
    [HttpGet("staff-shifts")]
    public async Task<IActionResult> GetStaffShifts([FromQuery] string? shiftCode, [FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await shiftService.GetStaffShiftsAsync(shiftCode, request, ct);
        return Ok(result);
    }
}
