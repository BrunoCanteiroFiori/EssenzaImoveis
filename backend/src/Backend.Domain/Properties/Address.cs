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

    private static string Required(string value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"O campo {field} é obrigatório.");
        }

        return Optional(value, field, maxLength)!;
    }

    private static string? Optional(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Trim().Length > maxLength)
        {
            throw new DomainException($"O campo {field} deve ter no máximo {maxLength} caracteres.");
        }

        return value.Trim();
    }

    private static string NormalizeState(string state)
    {
        var normalized = state?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!_states.Contains(normalized))
        {
            throw new DomainException("O campo Estado deve ser uma UF válida (ex.: SP).");
        }

        return normalized;
    }

    private static string NormalizeZipCode(string zipCode)
    {
        var digits = new string((zipCode ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != ZipCodeLength)
        {
            throw new DomainException($"O campo CEP deve ter {ZipCodeLength} dígitos.");
        }

        return digits;
    }
}
