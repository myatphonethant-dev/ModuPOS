using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ModuPOS.Shared.Results;

namespace ModuPOS.Shared.Pagination;

/// <summary>
/// Encapsulates pagination metadata including page numbers, page size, total record counts, and navigation flags.
/// </summary>
public class Pagination
{
    /// <summary>
    /// Gets the current 1-based page number.
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total count of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the total number of pages calculated from total count and page size.
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// Gets a value indicating whether there is a preceding page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether there is a subsequent page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pagination"/> class with explicit page count.
    /// </summary>
    /// <param name="pageNumber">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalCount">The total number of items.</param>
    /// <param name="totalPages">The total number of pages.</param>
    [JsonConstructor]
    public Pagination(int pageNumber, int pageSize, int totalCount, int totalPages)
    {
        PageNumber = Math.Max(1, pageNumber);
        PageSize = Math.Max(1, pageSize);
        TotalCount = Math.Max(0, totalCount);
        TotalPages = Math.Max(0, totalPages);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pagination"/> class, computing total pages automatically.
    /// </summary>
    /// <param name="pageNumber">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalCount">The total number of items.</param>
    public Pagination(int pageNumber, int pageSize, int totalCount)
    {
        PageNumber = Math.Max(1, pageNumber);
        PageSize = Math.Max(1, pageSize);
        TotalCount = Math.Max(0, totalCount);
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}

/// <summary>
/// Represents a paginated operational result enclosing a collection of data items along with pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the paginated collection.</typeparam>
public class PagedResult<T> : Result
{
    /// <summary>
    /// Gets the list of items for the current page.
    /// </summary>
    public List<T> Data { get; init; }

    /// <summary>
    /// Gets the pagination metadata for the result.
    /// </summary>
    public Pagination Pagination { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="message">The outcome message.</param>
    /// <param name="data">The collection of items.</param>
    /// <param name="pagination">The pagination metadata.</param>
    [JsonConstructor]
    public PagedResult(bool isSuccess, string message, List<T> data, Pagination pagination)
        : base(isSuccess, message)
    {
        Data = data;
        Pagination = pagination;
    }

    /// <summary>
    /// Creates a successful paginated result.
    /// </summary>
    /// <param name="data">The list of data items for the page.</param>
    /// <param name="pagination">The pagination metadata.</param>
    /// <param name="message">An optional confirmation message.</param>
    /// <returns>A successful <see cref="PagedResult{T}"/>.</returns>
    public static PagedResult<T> Success(List<T> data, Pagination pagination, string message = "Success")
        => new(true, message, data, pagination);

    /// <summary>
    /// Creates a failure paginated result.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <returns>A failed <see cref="PagedResult{T}"/>.</returns>
    public new static PagedResult<T> Failure(string message)
        => new(false, message, new List<T>(), new Pagination(1, 10, 0, 0));
}
