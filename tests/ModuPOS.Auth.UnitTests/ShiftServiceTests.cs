using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Services.Shift;
using ModuPOS.Shared.Pagination;

namespace ModuPOS.Auth.UnitTests;

/// <summary>
/// Unit tests for <see cref="ShiftService"/> verifying shift management and staff-shift assignment operations.
/// </summary>
public class ShiftServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateShiftAsync_ValidData_CreatesSuccessfully()
    {
        // Arrange
        await using var context = CreateContext();
        var service = new ShiftService(context);
        var request = new CreateShiftRequest("MORN-01", "Morning", DateTime.UtcNow, DateTime.UtcNow.AddHours(8), "admin");

        // Act
        var result = await service.CreateShiftAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("MORN-01", result.Data.ShiftCode);
        Assert.Equal("Morning", result.Data.ShiftType);

        var inDb = await context.Shifts.FirstOrDefaultAsync(s => s.ShiftCode == "MORN-01");
        Assert.NotNull(inDb);
    }

    [Fact]
    public async Task AssignStaffShiftAsync_ValidStaffAndShift_CreatesAssignment()
    {
        // Arrange
        await using var context = CreateContext();
        context.Staff.Add(new Staff
        {
            StaffId = 1,
            StaffCode = "STF-001",
            StaffName = "Kyaw Kyaw",
            DeleteFlag = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        context.Shifts.Add(new Shift
        {
            ShiftId = 1,
            ShiftCode = "MORN-01",
            ShiftType = "Morning",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new ShiftService(context);
        var request = new AssignStaffShiftRequest("SS-001", "STF-001", "MORN-01");

        // Act
        var result = await service.AssignStaffShiftAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("SS-001", result.Data.StaffShiftCode);
        Assert.Equal("STF-001", result.Data.StaffCode);
        Assert.Equal("MORN-01", result.Data.ShiftCode);
    }

    [Fact]
    public async Task AssignStaffShiftAsync_NonExistentStaff_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateContext();
        context.Shifts.Add(new Shift
        {
            ShiftId = 1,
            ShiftCode = "MORN-01",
            ShiftType = "Morning",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new ShiftService(context);
        var request = new AssignStaffShiftRequest("SS-999", "NON-EXISTENT", "MORN-01");

        // Act
        var result = await service.AssignStaffShiftAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStaffShiftsAsync_WithShiftCodeFilter_ReturnsMatchingAssignments()
    {
        // Arrange
        await using var context = CreateContext();
        context.Staff.Add(new Staff
        {
            StaffId = 1,
            StaffCode = "STF-001",
            StaffName = "Aung Aung",
            DeleteFlag = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        context.StaffShifts.Add(new StaffShift
        {
            StaffShiftId = 1,
            StaffShiftCode = "SS-001",
            StaffCode = "STF-001",
            ShiftCode = "MORN-01"
        });
        context.StaffShifts.Add(new StaffShift
        {
            StaffShiftId = 2,
            StaffShiftCode = "SS-002",
            StaffCode = "STF-001",
            ShiftCode = "EVE-01"
        });
        await context.SaveChangesAsync();

        var service = new ShiftService(context);
        var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetStaffShiftsAsync("MORN-01", request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("MORN-01", result.Data[0].ShiftCode);
        Assert.Equal("Aung Aung", result.Data[0].StaffName);
    }
}
