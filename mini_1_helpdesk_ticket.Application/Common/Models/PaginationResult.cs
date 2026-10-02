namespace mini_1_helpdesk_ticket.Application.Common.Models;

public sealed class PaginationResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }

    public PaginationResult(
        IReadOnlyList<T> items,
        int totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }
}
