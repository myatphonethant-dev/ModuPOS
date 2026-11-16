namespace ModuPOS.Auth.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    int UserId,
    string Username,
    int RoleId,
    string? RoleName,
    List<string> Permissions);

public record RefreshTokenRequest(string RefreshToken);

public record CreateUserRequest(
    string Username,
    string Password,
    int RoleId,
    string? Email = null,
    string? CreatedBy = null);

public record UpdateUserRequest(
    int RoleId,
    string? Email = null,
    bool IsActive = true,
    string? UpdatedBy = null);

public record UserResponseDto(
    int Id,
    int RoleId,
    string Username,
    string? Email,
    bool IsActive,
    DateTime CreatedAt,
    string? RoleName = null);

public record RoleDto(
    int Id,
    string Name,
    List<int> PermissionIds,
    List<string> PermissionNames);

public record CreateRoleRequest(string Name, List<int>? PermissionIds = null);

public record UpdateRoleRequest(string Name, List<int>? PermissionIds = null);

public record PermissionDto(int Id, string Name);
