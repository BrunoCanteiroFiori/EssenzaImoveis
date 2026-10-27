namespace Backend.Domain.Common;

/// <summary>
/// Página de resultados de uma consulta: itens da página atual e total de itens encontrados.
/// </summary>
public sealed record PagedList<TItem>(IReadOnlyList<TItem> Items, int TotalCount, int Page, int PageSize);
