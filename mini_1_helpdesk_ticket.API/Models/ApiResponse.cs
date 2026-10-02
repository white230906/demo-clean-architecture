namespace mini_1_helpdesk_ticket.API.Models;

public abstract class ApiResponse
{
    public bool IsSuccess { get; init; }
    public bool IsFailed => !IsSuccess;
    public object? Error { get; init; }
    public string? TraceId { get; init; }
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class BaseResponse<T> : ApiResponse
{
    public T? Value { get; init; }
}

public sealed class PaginationValue<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

public sealed class BasePaginationResponse<T> : ApiResponse
{
    public PaginationValue<T> Value { get; init; } = new();
}

public sealed class ErrorResponse
{
    public string Title { get; init; } = null!;
    public int Status { get; init; }
    public string Detail { get; init; } = null!;
    public string MessageCode { get; init; } = null!;
    public object? Errors { get; init; }
    public string? TraceId { get; init; }
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}

public static class ApiResponseFactory
{
    public static BaseResponse<T> Base<T>(
        T value,
        string? traceId = null)
    {
        return new BaseResponse<T>
        {
            IsSuccess = true,
            Value = value,
            TraceId = traceId
        };
    }

    public static BasePaginationResponse<T> BasePagination<T>(
        IReadOnlyList<T> items,
        int pageIndex,
        int pageSize,
        int totalCount,
        string? traceId = null)
    {
        var totalPages = pageSize == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new BasePaginationResponse<T>
        {
            IsSuccess = true,
            TraceId = traceId,
            Value = new PaginationValue<T>
            {
                Items = items,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = pageIndex < totalPages,
                HasPreviousPage = pageIndex > 1
            }
        };
    }

    public static ErrorResponse Error(
        string title,
        int status,
        string detail,
        string messageCode,
        object? errors,
        string? traceId = null)
    {
        return new ErrorResponse
        {
            Title = title,
            Status = status,
            Detail = detail,
            MessageCode = messageCode,
            Errors = errors,
            TraceId = traceId
        };
    }
}
