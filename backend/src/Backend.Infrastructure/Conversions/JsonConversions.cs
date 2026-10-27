using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Infrastructure.Conversions;

public static class JsonConversions
{
    /// <summary>
    /// Opções padrão de JSON do projeto: camelCase, sem diferenciar maiúsculas na leitura e enums como texto.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get; } = CreateDefaultOptions();

    public static string ToJson<TValue>(this TValue value) => JsonSerializer.Serialize(value, DefaultOptions);

    public static TValue? FromJson<TValue>(this string json) => JsonSerializer.Deserialize<TValue>(json, DefaultOptions);

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
