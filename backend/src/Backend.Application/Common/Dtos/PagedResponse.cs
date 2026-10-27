namespace Backend.Application.Common.Dtos;

public sealed record PagedResponse<TItem>(IReadOnlyList<TItem> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
