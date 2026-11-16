namespace ModuPOS.Auth.Interfaces;

using ModuPOS.Auth.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public interface IUserService
{
    Task<PagedResult<UserResponseDto>> GetUsersAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserResponseDto>> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<UserResponseDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserResponseDto>> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteUserAsync(int id, CancellationToken cancellationToken = default);
}
