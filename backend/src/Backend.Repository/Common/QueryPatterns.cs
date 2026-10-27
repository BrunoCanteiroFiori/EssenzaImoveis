namespace Backend.Repository.Common;

public static class QueryPatterns
{
    public const string LikeEscapeCharacter = "\\";

    /// <summary>
    /// Monta o padrão "contém" do ILIKE escapando <c>%</c>, <c>_</c> e a barra invertida digitados pelo usuário.
    /// </summary>
    public static string Contains(string text)
    {
        var escaped = text.Trim()
            .Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);

        return $"%{escaped}%";
    }
}
