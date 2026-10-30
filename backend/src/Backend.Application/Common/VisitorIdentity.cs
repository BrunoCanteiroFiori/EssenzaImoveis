using Backend.Infrastructure.Conversions;

namespace Backend.Application.Common;

/// <summary>
/// Identificador anônimo do visitante, gerado pelo navegador. Não há login público: é o que separa um visitante do outro.
/// </summary>
public static class VisitorIdentity
{
    public const int IdMinLength = 8;
    public const int IdMaxLength = 100;

    /// <summary>
    /// Hash gravado no banco no lugar do identificador.
    /// </summary>
    public static string ToVisitorHash(this string visitorId) => visitorId.Trim().ToSha256Hex();
}
