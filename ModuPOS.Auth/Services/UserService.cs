namespace ModuPOS.Auth.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.DTOs;
using ModuPOS.Auth.Interfaces;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public class UserService(AppDbContext context) : IUserService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<PagedResult<UserResponseDto>> GetUsersAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking().Where(x => !x.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await (
            from u in query
            join r in context.Roles.AsNoTracking().Where(x => !x.IsDeleted) on u.RoleId equals r.Id into roleGroup
            from role in roleGroup.DefaultIfEmpty()
            orderby u.Id
            select new UserResponseDto(u.Id, u.RoleId, u.Username, u.Email, u.IsActive, u.CreatedAt, role != null ? role.Name : null)
        )
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<UserResponseDto>.Success(items, pagination);
    }

    public async Task<Result<UserResponseDto>> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var u = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (u is null)
            return Result<UserResponseDto>.Failure("User not found.");

        var role = await context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == u.RoleId && !r.IsDeleted, cancellationToken);

        return Result<UserResponseDto>.Success(new UserResponseDto(u.Id, u.RoleId, u.Username, u.Email, u.IsActive, u.CreatedAt, role?.Name));
    }

    public async Task<Result<UserResponseDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Result<UserResponseDto>.Failure("Username and password are required.");

        var exists = await context.Users.AnyAsync(u => u.Username == request.Username && !u.IsDeleted, cancellationToken);
        if (exists)
            return Result<UserResponseDto>.Failure($"Username '{request.Username}' is already taken.");

        var roleExists = await context.Roles.AnyAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken);
        if (!roleExists)
            return Result<UserResponseDto>.Failure("Specified role does not exist.");

        var user = new User
        {
            Username = request.Username,
            RoleId = request.RoleId,
            Email = request.Email,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        var role = await context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == user.RoleId, cancellationToken);

        return Result<UserResponseDto>.Success(new UserResponseDto(user.Id, user.RoleId, user.Username, user.Email, user.IsActive, user.CreatedAt, role?.Name));
    }

    public async Task<Result<UserResponseDto>> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user is null)
            return Result<UserResponseDto>.Failure("User not found.");

        var roleExists = await context.Roles.AnyAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken);
        if (!roleExists)
            return Result<UserResponseDto>.Failure("Specified role does not exist.");

        user.RoleId = request.RoleId;
        user.Email = request.Email;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = request.UpdatedBy;

        await context.SaveChangesAsync(cancellationToken);

        var role = await context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == user.RoleId, cancellationToken);

        return Result<UserResponseDto>.Success(new UserResponseDto(user.Id, user.RoleId, user.Username, user.Email, user.IsActive, user.CreatedAt, role?.Name));
    }

    public async Task<Result> DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user is null)
            return Result.Failure("User not found.");

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success("User deleted successfully.");
    }
}
