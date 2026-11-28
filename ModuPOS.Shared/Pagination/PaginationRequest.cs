namespace ModuPOS.Shared.Pagination;

public class PaginationRequest
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}