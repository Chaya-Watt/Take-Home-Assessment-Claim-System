namespace ClaimSystem.Application.Common;

/// <summary>One page of a list. <see cref="Page"/> starts at 1.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public const int MaxPageSize = 100;

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> map) => new([.. Items.Select(map)], Page, PageSize, TotalCount);
}
