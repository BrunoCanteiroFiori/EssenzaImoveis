using Backend.Application.Common.Dtos;
using Backend.Domain.Common;

namespace Backend.Application.Common;

public static class PagedListMappings
{
    public static PagedResponse<TItem> ToResponse<TSource, TItem>(this PagedList<TSource> pagedList, Func<TSource, TItem> map) =>
        new([.. pagedList.Items.Select(map)], pagedList.TotalCount, pagedList.Page, pagedList.PageSize);
}
