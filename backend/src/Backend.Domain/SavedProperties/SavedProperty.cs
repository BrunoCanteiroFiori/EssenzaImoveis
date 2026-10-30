using Backend.Domain.Common;
using Backend.Domain.PropertyViews;

namespace Backend.Domain.SavedProperties;

/// <summary>
/// Imóvel guardado por um visitante para ver depois. Não há login público: o visitante é o mesmo hash anônimo de
/// <see cref="PropertyView"/>. O imóvel continua na lista mesmo depois de sair do site (vendido, alugado, pausado).
/// </summary>
public sealed class SavedProperty : AggregateRoot
{
    private SavedProperty()
    {
    }

    public Guid PropertyId { get; private set; }

    public string VisitorHash { get; private set; } = string.Empty;

    public DateTimeOffset SavedAt { get; private set; }

    public static SavedProperty Create(Guid propertyId, string visitorHash, DateTimeOffset savedAt)
    {
        if (propertyId == Guid.Empty)
        {
            throw new DomainException("O campo PropertyId é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(visitorHash) || visitorHash.Length != PropertyView.VisitorHashLength)
        {
            throw new DomainException("Identificador de visitante inválido.");
        }

        return new SavedProperty
        {
            PropertyId = propertyId,
            VisitorHash = visitorHash,
            SavedAt = savedAt.ToUniversalTime(),
        };
    }
}
