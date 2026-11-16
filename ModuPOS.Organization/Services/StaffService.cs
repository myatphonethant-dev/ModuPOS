namespace ModuPOS.Organization.Services;

using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Interfaces;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public class StaffService(AppDbContext context) : IStaffService
{
    public async Task<PagedResult<StaffResponseDto>> GetStaffListAsync(string? branchCode = null, PaginationRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new PaginationRequest();

        var query = context.Staff
            .AsNoTracking()
            .Where(s => !s.DeleteFlag);

        if (!string.IsNullOrWhiteSpace(branchCode))
            query = query.Where(s => s.BranchCode == branchCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.StaffId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StaffResponseDto(
                s.StaffId, s.StaffCode, s.StaffName, s.DateOfBirth, s.Phone, s.Email, s.Address, s.BranchCode, s.IsActive, s.CreatedAt))
            .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<StaffResponseDto>.Success(items, pagination);
    }

    public async Task<Result<StaffResponseDto>> GetStaffByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var s = await context.Staff
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StaffId == id && !x.DeleteFlag, cancellationToken);

        if (s is null)
            return Result<StaffResponseDto>.Failure("Staff not found.");

        return Result<StaffResponseDto>.Success(new StaffResponseDto(
            s.StaffId, s.StaffCode, s.StaffName, s.DateOfBirth, s.Phone, s.Email, s.Address, s.BranchCode, s.IsActive, s.CreatedAt));
    }

    public async Task<Result<StaffResponseDto>> CreateStaffAsync(CreateStaffRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.StaffCode) || string.IsNullOrWhiteSpace(request.StaffName))
            return Result<StaffResponseDto>.Failure("Staff code and name are required.");

        var exists = await context.Staff.AnyAsync(s => s.StaffCode == request.StaffCode && !s.DeleteFlag, cancellationToken);
        if (exists)
            return Result<StaffResponseDto>.Failure($"Staff with code '{request.StaffCode}' already exists.");

        var entity = new Staff
        {
            StaffCode = request.StaffCode,
            StaffName = request.StaffName,
            DateOfBirth = request.DateOfBirth,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            BranchCode = request.BranchCode,
            IsActive = true,
            DeleteFlag = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        context.Staff.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result<StaffResponseDto>.Success(new StaffResponseDto(
            entity.StaffId, entity.StaffCode, entity.StaffName, entity.DateOfBirth, entity.Phone, entity.Email, entity.Address, entity.BranchCode, entity.IsActive, entity.CreatedAt));
    }

    public async Task<Result<StaffResponseDto>> UpdateStaffAsync(int id, UpdateStaffRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await context.Staff.FirstOrDefaultAsync(s => s.StaffId == id && !s.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result<StaffResponseDto>.Failure("Staff not found.");

        entity.StaffName = request.StaffName;
        entity.DateOfBirth = request.DateOfBirth;
        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.Address = request.Address;
        entity.BranchCode = request.BranchCode;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy;

        await context.SaveChangesAsync(cancellationToken);

        return Result<StaffResponseDto>.Success(new StaffResponseDto(
            entity.StaffId, entity.StaffCode, entity.StaffName, entity.DateOfBirth, entity.Phone, entity.Email, entity.Address, entity.BranchCode, entity.IsActive, entity.CreatedAt));
    }

    public async Task<Result> DeleteStaffAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Staff.FirstOrDefaultAsync(s => s.StaffId == id && !s.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result.Failure("Staff not found.");

        entity.DeleteFlag = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success("Staff deleted successfully.");
    }
}
