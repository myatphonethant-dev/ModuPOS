
using ModuPOS.Delivery.DTOs;
using ModuPOS.Delivery.Enums;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

namespace ModuPOS.Delivery.Interfaces;

public interface ICustomerOrderService
{
    Task<Result<CreatedCustomerOrderResponse>> CreateAsync(
        CreateCustomerOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<CustomerOrderResponse>> GetAllAsync(
        int? branchId,
        CustomerOrderStatus? status,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerOrderResponse>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerOrderResponse>> UpdateStatusAsync(
        string id,
        CustomerOrderStatus status,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerOrderResponse>> AssignRiderAsync(
        string id,
        AssignRiderRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerOrderResponse>> VerifyPickupAsync(
        string id,
        string pickupCode,
        CancellationToken cancellationToken = default);
}