namespace Backend.Api.Visitors;

/// <summary>
/// O identificador anônimo do visitante vai em cabeçalho, e não na URL, para não ficar em logs nem no histórico.
/// </summary>
public static class VisitorHeaders
{
    public const string Id = "X-Visitor-Id";
}
