using Backend.Domain.Common;

namespace Backend.Domain.Properties;

public sealed class Address : ValueObject
{
    public const int StreetMaxLength = 200;
    public const int NumberMaxLength = 20;
    public const int ComplementMaxLength = 100;
    public const int NeighborhoodMaxLength = 100;
    public const int CityMaxLength = 100;
    public const int StateLength = 2;
    public const int ZipCodeLength = 8;

    private static readonly HashSet<string> _states =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
        "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    ];

    private Address()
    {
    }

    public string Street { get; private set; } = string.Empty;

    public string? Number { get; private set; }

    public string? Complement { get; private set; }

    public string Neighborhood { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    public string ZipCode { get; private set; } = string.Empty;

    public static Address Create(
        string street,
        string? number,
        string? complement,
        string neighborhood,
        string city,
        string state,
        string zipCode)
    {
        return new Address
        {
            Street = Required(street, "Logradouro", StreetMaxLength),
            Number = Optional(number, "Número", NumberMaxLength),
            Complement = Optional(complement, "Complemento", ComplementMaxLength),
            Neighborhood = Required(neighborhood, "Bairro", NeighborhoodMaxLength),
            City = Required(city, "Cidade", CityMaxLength),
            State = NormalizeState(state),
            ZipCode = NormalizeZipCode(zipCode),
        };
    }

    /// <summary>
    /// Valida o CEP e devolve só os 8 dígitos (aceita máscara, ex.: 01001-000).
    /// Só vale dígito ASCII: dígitos de outros alfabetos (ex.: largura total) são ignorados.
    /// Lança <see cref="DomainException"/> se o CEP for inválido.
    /// </summary>
    public static string NormalizeZipCode(string zipCode)
    {
        var digits = new string((zipCode ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (digits.Length != ZipCodeLength)
        {
            throw new DomainException($"O campo CEP deve ter {ZipCodeLength} dígitos.");
        }

        return digits;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return Number;
        yield return Complement;
        yield return Neighborhood;
        yield return City;
        yield return State;
        yield return ZipCode;
    }

    private static string Required(string value, string field, int maxLength) =>
        Optional(value, field, maxLength) ?? throw new DomainException($"O campo {field} é obrigatório.");

    private static string? Optional(string? value, string field, int maxLength)
    {
        var normalized = TextNormalizer.NormalizeLine(value);
        if (normalized.Length == 0)
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"O campo {field} deve ter no máximo {maxLength} caracteres.");
        }

        return normalized;
    }

    private static string NormalizeState(string state)
    {
        var normalized = TextNormalizer.NormalizeLine(state).ToUpperInvariant();
        if (!_states.Contains(normalized))
        {
            throw new DomainException("O campo Estado deve ser uma UF válida (ex.: SP).");
        }

        return normalized;
    }
}
