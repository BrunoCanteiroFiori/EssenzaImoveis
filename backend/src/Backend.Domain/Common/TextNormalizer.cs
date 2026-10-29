using System.Globalization;
using System.Text;

namespace Backend.Domain.Common;

/// <summary>
/// Limpa texto digitado, colado ou vindo de serviço externo antes de ir para o banco:
/// remove caracteres de controle (o PostgreSQL rejeita <c>\0</c>), invisíveis (espaço de largura zero, BOM, marcas de direção)
/// e surrogates soltos (UTF-8 inválido), troca espaços especiais (ex.: espaço não separável) por espaço comum,
/// junta espaços repetidos, apara as pontas e compõe os acentos (NFC), para "São Paulo" colado de fontes diferentes ser o mesmo texto.
/// </summary>
public static class TextNormalizer
{
    private const char ZeroWidthJoiner = '\u200D';
    private const int MaxConsecutiveLineBreaks = 2;

    /// <summary>
    /// Normaliza texto de uma linha (título, endereço): quebras de linha e tabulações viram espaço.
    /// Retorna texto vazio para nulo ou só espaços.
    /// </summary>
    public static string NormalizeLine(string? value) => Normalize(value, keepLineBreaks: false);

    /// <summary>
    /// Normaliza texto com parágrafos (descrição): mantém as quebras de linha como <c>\n</c>,
    /// com no máximo uma linha em branco seguida, e preserva emojis compostos.
    /// </summary>
    public static string NormalizeMultiline(string? value) => Normalize(value, keepLineBreaks: true);

    private static string Normalize(string? value, bool keepLineBreaks)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;
        var pendingLineBreaks = 0;

        void AppendPendingSeparator()
        {
            if (builder.Length > 0 && pendingLineBreaks > 0)
            {
                builder.Append('\n', Math.Min(pendingLineBreaks, MaxConsecutiveLineBreaks));
            }
            else if (builder.Length > 0 && pendingSpace)
            {
                builder.Append(' ');
            }

            pendingSpace = false;
            pendingLineBreaks = 0;
        }

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            if (char.IsHighSurrogate(character) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
            {
                AppendPendingSeparator();
                builder.Append(character).Append(value[++index]);
                continue;
            }

            if (char.IsSurrogate(character) || (character == '\r' && index + 1 < value.Length && value[index + 1] == '\n'))
            {
                continue;
            }

            if (IsLineBreak(character) && keepLineBreaks)
            {
                pendingLineBreaks++;
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                pendingSpace = true;
                continue;
            }

            if (IsInvisible(character, keepLineBreaks))
            {
                continue;
            }

            AppendPendingSeparator();
            builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool IsLineBreak(char character) => character is '\n' or '\r' or '\u0085' or '\u2028' or '\u2029';

    private static bool IsInvisible(char character, bool keepZeroWidthJoiner) =>
        char.GetUnicodeCategory(character) switch
        {
            UnicodeCategory.Control => true,
            UnicodeCategory.Format => !(keepZeroWidthJoiner && character == ZeroWidthJoiner),
            _ => false,
        };
}
