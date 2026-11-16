namespace ModuPOS.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Interfaces;
using ModuPOS.Shared.Pagination;

[ApiController]
[Route("api/[controller]")]
public class ShiftsController(IShiftService shiftService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await shiftService.GetShiftsAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await shiftService.GetShiftByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShiftRequest request, CancellationToken ct)
    {
        var result = await shiftService.CreateShiftAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data!.ShiftId }, result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShiftRequest request, CancellationToken ct)
    {
        var result = await shiftService.UpdateShiftAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignStaffShift([FromBody] AssignStaffShiftRequest request, CancellationToken ct)
    {
        var result = await shiftService.AssignStaffShiftAsync(request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("staff-shifts")]
    public async Task<IActionResult> GetStaffShifts([FromQuery] string? shiftCode, [FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await shiftService.GetStaffShiftsAsync(shiftCode, request, ct);
        return Ok(result);
    }
}
