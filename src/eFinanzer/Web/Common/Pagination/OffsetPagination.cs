using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Common.Pagination;

public record OffsetPagination(int PageSize, int Offset = 0)
{
    public bool IsValid(int maxPageSize) => PageSize > 0 && Offset >= 0 && PageSize <= maxPageSize;
}

public static class QueryableExtensions
{
    public static async Task<Page<TItem>> ToPage<TItem>(this IOrderedQueryable<TItem> query, OffsetPagination pagination, CancellationToken ct = default)
    {
        var items = await query.Skip(pagination.Offset).Take(pagination.PageSize).ToArrayAsync(ct);
        var total = await query.CountAsync(ct);
        
        return new Page<TItem>(items, total);
    }
}