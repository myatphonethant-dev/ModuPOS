namespace ModuPOS.Organization.Services;

using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Interfaces;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public class StaffService : IStaffService
{
    private readonly AppDbContext _context;

    public StaffService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<StaffResponseDto>> GetStaffListAsync(string? branchCode = null, PaginationRequest? request = null, CancellationToken cancellationToken = default)
    {
        if (request is null) return PagedResult<StaffResponseDto>.Failure("Request cannot be null");

        var query = _context.Staff
            .AsNoTracking()
            .Where(s => !s.DeleteFlag);

        if (!string.IsNullOrWhiteSpace(branchCode))
            query = query.Where(s => s.BranchCode == branchCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.StaffId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StaffResponseDto
            {
                StaffId = s.StaffId,
                StaffCode = s.StaffCode,
                StaffName = s.StaffName,
                DateOfBirth = s.DateOfBirth,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                BranchCode = s.BranchCode,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<StaffResponseDto>.Success(items, pagination);
    }

    public async Task<Result<StaffResponseDto>> GetStaffByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var s = await _context.Staff
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StaffId == id && !x.DeleteFlag, cancellationToken);

        if (s is null)
            return Result<StaffResponseDto>.Failure("Staff not found.");

        return Result<StaffResponseDto>.Success(new StaffResponseDto
        {
            StaffId = s.StaffId,
            StaffCode = s.StaffCode,
            StaffName = s.StaffName,
            DateOfBirth = s.DateOfBirth,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            BranchCode = s.BranchCode,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        });
    }

    public async Task<Result<StaffResponseDto>> CreateStaffAsync(CreateStaffRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.StaffCode) || string.IsNullOrWhiteSpace(request.StaffName))
            return Result<StaffResponseDto>.Failure("Staff code and name are required.");

        var exists = await _context.Staff.AnyAsync(s => s.StaffCode == request.StaffCode && !s.DeleteFlag, cancellationToken);
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

        _context.Staff.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<StaffResponseDto>.Success(new StaffResponseDto
        {
            StaffId = entity.StaffId,
            StaffCode = entity.StaffCode,
            StaffName = entity.StaffName,
            DateOfBirth = entity.DateOfBirth,
            Phone = entity.Phone,
            Email = entity.Email,
            Address = entity.Address,
            BranchCode = entity.BranchCode,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        });
    }

    public async Task<Result<StaffResponseDto>> UpdateStaffAsync(int id, UpdateStaffRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Staff.FirstOrDefaultAsync(s => s.StaffId == id && !s.DeleteFlag, cancellationToken);
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

        await _context.SaveChangesAsync(cancellationToken);

        return Result<StaffResponseDto>.Success(new StaffResponseDto
        {
            StaffId = entity.StaffId,
            StaffCode = entity.StaffCode,
            StaffName = entity.StaffName,
            DateOfBirth = entity.DateOfBirth,
            Phone = entity.Phone,
            Email = entity.Email,
            Address = entity.Address,
            BranchCode = entity.BranchCode,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        });
    }

    public async Task<Result> DeleteStaffAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Staff.FirstOrDefaultAsync(s => s.StaffId == id && !s.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result.Failure("Staff not found.");

        entity.DeleteFlag = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success("Staff deleted successfully.");
    }
}
