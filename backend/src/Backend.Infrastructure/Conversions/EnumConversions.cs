using System.ComponentModel;
using System.Reflection;

namespace Backend.Infrastructure.Conversions;

public static class EnumConversions
{
    /// <summary>
    /// Retorna o texto do atributo <see cref="DescriptionAttribute"/> do valor ou, se não houver, o nome do valor.
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var name = value.ToString();
        var field = value.GetType().GetField(name);
        return field?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name;
    }

    /// <summary>
    /// Converte um texto (nome do valor, sem diferenciar maiúsculas) para o enum. Lança <see cref="ArgumentException"/> se não existir.
    /// </summary>
    public static TEnum ToEnum<TEnum>(this string value)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) && Enum.IsDefined(result))
        {
            return result;
        }

        throw new ArgumentException($"O valor '{value}' não é válido para {typeof(TEnum).Name}.", nameof(value));
    }
}
