using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shared.Application;

public sealed record PagedRequest(int Page = 1, int PageSize = 20)
{
    public int Page { get; init; } = Page < 1 ? 1 : Page;
    public int PageSize { get; init; } = PageSize < 1 ? 20 : PageSize > 100 ? 100 : PageSize;

    public int Skip => (Page - 1) * PageSize;
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public static class PaginationExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedRequest paging,
        CancellationToken ct = default)
    {
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, totalCount, paging.Page, paging.PageSize);
    }
}
