namespace mini_1_helpdesk_ticket.Service.Models;

using System;
using System.Collections;
using System.Collections.Generic;

public abstract class ApiResponse
{
    public bool IsSuccess { get; set; }
    public bool IsFailed { get; set; }
    public object? Error { get; set; }
    public string? TraceId { get; set; }
    public DateTime TimestampUtc { get; set; }
}

public class BaseResponse : ApiResponse
{
    public object? Value { get; set; }
}

public class PaginationValue
{
    public List<object> Items { get; set; } = new();
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class BasePaginationResponse : ApiResponse
{
    public PaginationValue Value { get; set; } = new();
}

public class ErrorResponse
{
    public string Title { get; set; } = null!;
    public int Status { get; set; }
    public string Detail { get; set; } = null!;
    public string MessageCode { get; set; } = null!;
    public object? Errors { get; set; }
    public string? TraceId { get; set; }
    public DateTime TimestampUtc { get; set; }
}

public static class ApiResponseFactory
{
    public static BaseResponse Base(
        object? data, 
        bool isSuccess = true, 
        object? errorCustom = null, 
        string? traceId = null)
    {
        return new BaseResponse
        {
            IsSuccess = isSuccess,
            IsFailed = !isSuccess,
            Value = isSuccess ? data : null,
            Error = !isSuccess ? errorCustom : null,
            TraceId = traceId,
            TimestampUtc = DateTime.UtcNow
        };
    }

    public static BasePaginationResponse BasePagination(
        IList? items, 
        int pageIndex, 
        int pageSize, 
        int totalCount)
    {
        var newItems = new List<object>();
        if (items != null)
        {
            foreach (var item in items)
            {
                newItems.Add(item);
            }
        }

        return new BasePaginationResponse
        {
            IsSuccess = true,
            IsFailed = false,
            Error = null,
            Value = new PaginationValue
            {
                Items = newItems,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                HasNextPage = pageIndex * pageSize < totalCount,
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
            TraceId = traceId,
            TimestampUtc = DateTime.UtcNow
        };
    }
}