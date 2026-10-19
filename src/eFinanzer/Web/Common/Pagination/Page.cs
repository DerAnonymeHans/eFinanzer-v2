using System.Collections;

namespace eFinanzer.Common.Pagination;

public record Page<TItem>(ICollection<TItem> Items, int TotalCount)
{
    public Page<TNewItem> Select<TNewItem>(Func<TItem, TNewItem> selector) => new(Items.Select(selector).ToArray(), TotalCount);
}