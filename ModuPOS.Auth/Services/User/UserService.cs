using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.DTOs;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Shared.Results;

namespace ModuPOS.Auth.Services.User;

public interface IUserService
{
    Task<Result<List<UserResponseDto>>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<Result<UserResponseDto>> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<UserResponseDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserResponseDto>> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteUserAsync(int id, CancellationToken cancellationToken = default);
}

public class UserService(AppDbContext context) : IUserService
{
    private readonly PasswordHasher<Database.AppDbContextModels.User> _passwordHasher = new();

    public async Task<Result<List<UserResponseDto>>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await (
            from u in context.Users.AsNoTracking().Where(x => !x.IsDeleted)
            join r in context.Roles.AsNoTracking().Where(x => !x.IsDeleted) on u.RoleId equals r.Id into roleGroup
            from role in roleGroup.DefaultIfEmpty()
            select new UserResponseDto(u.Id, u.RoleId, u.Username, u.Email, u.IsActive, u.CreatedAt, role != null ? role.Name : null)
        ).ToListAsync(cancellationToken);

        return Result.Success(users);
    }

    public async Task<Result<UserResponseDto>> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var u = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (u is null)
            return Result.Failure<UserResponseDto>("User not found.");

        var role = await context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == u.RoleId && !r.IsDeleted, cancellationToken);

        return Result.Success(new UserResponseDto(u.Id, u.RoleId, u.Username, u.Email, u.IsActive, u.CreatedAt, role?.Name));
    }

    public async Task<Result<UserResponseDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Result.Failure<UserResponseDto>("Username and password are required.");

        var exists = await context.Users.AnyAsync(u => u.Username == request.Username && !u.IsDeleted, cancellationToken);
        if (exists)
            return Result.Failure<UserResponseDto>($"Username '{request.Username}' is already taken.");

        var roleExists = await context.Roles.AnyAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken);
        if (!roleExists)
            return Result.Failure<UserResponseDto>("Specified role does not exist.");

        var user = new Database.AppDbContextModels.User
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

        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<Result<UserResponseDto>> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user is null)
            return Result.Failure<UserResponseDto>("User not found.");

        var roleExists = await context.Roles.AnyAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken);
        if (!roleExists)
            return Result.Failure<UserResponseDto>("Specified role does not exist.");

        user.RoleId = request.RoleId;
        user.Email = request.Email;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = request.UpdatedBy;

        await context.SaveChangesAsync(cancellationToken);

        return await GetUserByIdAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user is null)
            return Result.Failure("User not found.");

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
