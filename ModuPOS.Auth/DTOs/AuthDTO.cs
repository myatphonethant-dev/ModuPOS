namespace ModuPOS.Auth.DTOs;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = new List<string>();
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    public int RoleId { get; set; }

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string UpdatedBy { get; set; } = string.Empty;
}

public class UserResponseDto
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public string RoleName { get; set; } = string.Empty;
}

public class RoleDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<int> PermissionIds { get; set; } = new List<int>();

    public List<string> PermissionNames { get; set; } = new List<string>();
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;

    public List<int>? PermissionIds { get; set; } = null;
}

public class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;

    public List<int>? PermissionIdsToAdd { get; set; } = null;

    public List<int>? PermissionIdsToRemove { get; set; } = null;
}

public class PermissionDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}