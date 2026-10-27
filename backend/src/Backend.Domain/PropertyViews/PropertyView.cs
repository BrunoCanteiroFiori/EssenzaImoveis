using Backend.Domain.Common;

namespace Backend.Domain.PropertyViews;

/// <summary>
/// Acesso de um visitante a um imóvel. Cada visitante conta no máximo uma vez por imóvel por dia.
/// </summary>
public sealed class PropertyView : AggregateRoot
{
    public const int VisitorHashLength = 64;

    private PropertyView()
    {
    }

    public Guid PropertyId { get; private set; }

    public string VisitorHash { get; private set; } = string.Empty;

    public DateOnly ViewedOn { get; private set; }

    public DateTimeOffset ViewedAt { get; private set; }

    public static PropertyView Create(Guid propertyId, string visitorHash, DateTimeOffset viewedAt)
    {
        if (propertyId == Guid.Empty)
        {
            throw new DomainException("O campo PropertyId é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(visitorHash) || visitorHash.Length != VisitorHashLength)
        {
            throw new DomainException("Identificador de visitante inválido.");
        }

        var utc = viewedAt.ToUniversalTime();
        return new PropertyView
        {
            PropertyId = propertyId,
            VisitorHash = visitorHash,
            ViewedAt = utc,
            ViewedOn = DateOnly.FromDateTime(utc.UtcDateTime),
        };
    }
}
