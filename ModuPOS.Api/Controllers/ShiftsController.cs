using Microsoft.AspNetCore.Mvc;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Services.Shift;

namespace ModuPOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShiftsController(IShiftService shiftService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await shiftService.GetShiftsAsync(ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShiftRequest request, CancellationToken ct)
    {
        var result = await shiftService.CreateShiftAsync(request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
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
    public async Task<IActionResult> GetStaffShifts([FromQuery] string? shiftCode, CancellationToken ct)
    {
        var result = await shiftService.GetStaffShiftsAsync(shiftCode, ct);
        return Ok(result);
    }
}
