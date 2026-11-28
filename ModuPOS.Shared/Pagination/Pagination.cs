namespace ModuPOS.Shared.Pagination;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ModuPOS.Shared.Results;

public class Pagination
{
    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    [JsonConstructor]
    public Pagination(int pageNumber, int pageSize, int totalCount, int totalPages)
    {
        PageNumber = Math.Max(1, pageNumber);
        PageSize = Math.Max(1, pageSize);
        TotalCount = Math.Max(0, totalCount);
        TotalPages = Math.Max(0, totalPages);
    }

    public Pagination(int pageNumber, int pageSize, int totalCount)
    {
        PageNumber = Math.Max(1, pageNumber);
        PageSize = Math.Max(1, pageSize);
        TotalCount = Math.Max(0, totalCount);
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}

[method: JsonConstructor]
public class PagedResult<T>(bool isSuccess, string message, List<T> data, Pagination pagination) : Result(isSuccess, message)
{
    public List<T> Data { get; init; } = data;

    public Pagination Pagination { get; init; } = pagination;

    public static PagedResult<T> Success(List<T> data, Pagination pagination, string message = "Success")
        => new(true, message, data, pagination);

    public new static PagedResult<T> Failure(string message)
        => new(false, message, new List<T>(), new Pagination(1, 10, 0, 0));
}