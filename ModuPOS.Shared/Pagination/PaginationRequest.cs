namespace ModuPOS.Shared.Pagination;

/// <summary>
/// Represents incoming request parameters for paginated collection queries.
/// </summary>
public class PaginationRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 10;

    /// <summary>
    /// Gets or sets the 1-based page number (defaults to 1).
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Gets or sets the number of records per page, bounded between 1 and 100 (defaults to 10).
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
    }
}
