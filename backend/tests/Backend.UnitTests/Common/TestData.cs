using Backend.Application.Properties.Dtos;
using Backend.Domain.Properties;
using Backend.Domain.Users;

namespace Backend.UnitTests.Common;

/// <summary>
/// Valores de teste reutilizados pelos testes de domínio e de services.
/// </summary>
internal static class TestData
{
    public const string PasswordHash = "hash-de-teste";
    public const string ValidPassword = "SenhaForte123";

    public static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public static Address Address() => Domain.Properties.Address.Create(
        "Rua das Flores", "100", "Apto 12", "Centro", "São Paulo", "sp", "01001-000");

    public static AddressRequest AddressRequest() =>
        new("Rua das Flores", "100", null, "Centro", "São Paulo", "SP", "01001-000");

    public static Property Property(
        PropertyPurpose purpose = PropertyPurpose.Sale,
        PropertyType type = PropertyType.House,
        string title = "Casa com quintal") =>
        Domain.Properties.Property.Create(
            title,
            "Casa ampla e iluminada.",
            type,
            purpose,
            500_000m,
            null,
            1_200m,
            150m,
            type == PropertyType.Land ? 0 : 3,
            type == PropertyType.Land ? 0 : 2,
            2,
            Address(),
            Now);

    public static CreatePropertyRequest CreatePropertyRequest(string title = "Casa com quintal") =>
        new(title, "Casa ampla e iluminada.", PropertyType.House, PropertyPurpose.Sale, 500_000m, null, 1_200m, 150m, 3, 2, 2, AddressRequest());

    public static User Staff(UserPermissions permissions = UserPermissions.ReadProperties) =>
        User.Create("Maria Silva", "maria@corretora.com.br", PasswordHash, permissions, Now);

    public static User SuperAdmin() =>
        User.CreateSuperAdmin("Super Admin", "admin@corretora.com.br", PasswordHash, Now);
}
