using System.Globalization;
using System.Text;

namespace Backend.Infrastructure.Conversions;

public static class StringConversions
{
    /// <summary>
    /// Converte PascalCase, camelCase, kebab-case ou texto com espaços para snake_case (ex.: UserProfile → user_profile).
    /// </summary>
    public static string ToSnakeCase(this string value) => ToSeparatedLowerCase(value, '_');

    /// <summary>
    /// Converte PascalCase, camelCase, snake_case ou texto com espaços para kebab-case (ex.: UserProfiles → user-profiles).
    /// </summary>
    public static string ToKebabCase(this string value) => ToSeparatedLowerCase(value, '-');

    /// <summary>
    /// Remove acentos e cedilha (ex.: "Relatório de transações" → "Relatorio de transacoes").
    /// </summary>
    public static string RemoveDiacritics(this string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string ToSeparatedLowerCase(string value, char separator)
    {
        var builder = new StringBuilder(value.Length + 8);

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            if (character is '-' or '_' or ' ')
            {
                if (builder.Length > 0 && builder[^1] != separator)
                {
                    builder.Append(separator);
                }

                continue;
            }

            if (char.IsUpper(character) && builder.Length > 0 && builder[^1] != separator)
            {
                var previous = value[index - 1];
                var nextIsLower = index + 1 < value.Length && char.IsLower(value[index + 1]);
                if (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && nextIsLower))
                {
                    builder.Append(separator);
                }
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
