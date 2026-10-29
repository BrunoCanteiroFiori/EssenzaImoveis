using Backend.Domain.Common;

namespace Backend.Domain.Properties;

public sealed class Property : AggregateRoot
{
    public const int TitleMaxLength = 150;
    public const int DescriptionMaxLength = 5000;
    public const int MediaFileNameMaxLength = 100;
    public const int MaxMedia = 30;
    public const int MaxVideos = 5;
    public const int MaxRooms = 99;

    private static readonly string[] _videoExtensions = [".mp4", ".webm"];

    private List<string> _media = [];

    private Property()
    {
    }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public PropertyType Type { get; private set; }

    public PropertyPurpose Purpose { get; private set; }

    public PropertyStatus Status { get; private set; }

    public decimal Price { get; private set; }

    public decimal? CondominiumFee { get; private set; }

    public decimal? PropertyTax { get; private set; }

    public decimal Area { get; private set; }

    public int Bedrooms { get; private set; }

    public int Bathrooms { get; private set; }

    public int ParkingSpaces { get; private set; }

    public Address Address { get; private set; } = null!;

    /// <summary>
    /// Fotos e vídeos na ordem do carrossel (nomes de arquivo).
    /// </summary>
    public IReadOnlyList<string> Media => _media.AsReadOnly();

    /// <summary>
    /// Capa do anúncio: a primeira foto do carrossel. Vídeo nunca é capa.
    /// </summary>
    public string? CoverImage => _media.FirstOrDefault(fileName => GetMediaType(fileName) == PropertyMediaType.Photo);

    public DateTimeOffset? SponsoredUntil { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsPubliclyVisible => Status == PropertyStatus.Available;

    public static Property Create(
        string title,
        string description,
        PropertyType type,
        PropertyPurpose purpose,
        decimal price,
        decimal? condominiumFee,
        decimal? propertyTax,
        decimal area,
        int bedrooms,
        int bathrooms,
        int parkingSpaces,
        Address address,
        DateTimeOffset now)
    {
        var property = new Property
        {
            Status = PropertyStatus.Available,
            CreatedAt = now.ToUniversalTime(),
        };

        property.Update(title, description, type, purpose, price, condominiumFee, propertyTax, area, bedrooms, bathrooms, parkingSpaces, address, now);
        return property;
    }

    public void Update(
        string title,
        string description,
        PropertyType type,
        PropertyPurpose purpose,
        decimal price,
        decimal? condominiumFee,
        decimal? propertyTax,
        decimal area,
        int bedrooms,
        int bathrooms,
        int parkingSpaces,
        Address address,
        DateTimeOffset now)
    {
        SetTitle(title);
        SetDescription(description);
        SetType(type);
        SetPurpose(purpose);
        SetPrice(price);
        SetCondominiumFee(condominiumFee);
        SetPropertyTax(propertyTax);
        SetArea(area);
        SetRooms(bedrooms, bathrooms, parkingSpaces);
        Address = address ?? throw new DomainException("O endereço é obrigatório.");
        Touch(now);
    }

    /// <summary>
    /// Altera o status. Vendido só vale para imóvel à venda e alugado só para imóvel para alugar.
    /// Qualquer status diferente de disponível encerra o patrocínio.
    /// </summary>
    public void ChangeStatus(PropertyStatus status, DateTimeOffset now)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Status inválido.");
        }

        EnsureStatusMatchesPurpose(status, Purpose);
        Status = status;
        if (status != PropertyStatus.Available)
        {
            SponsoredUntil = null;
        }

        Touch(now);
    }

    public void Sponsor(DateTimeOffset until, DateTimeOffset now)
    {
        if (Status != PropertyStatus.Available)
        {
            throw new DomainException("Somente imóveis disponíveis podem ser patrocinados.");
        }

        if (until <= now)
        {
            throw new DomainException("O fim do patrocínio deve ser uma data futura.");
        }

        SponsoredUntil = until.ToUniversalTime();
        Touch(now);
    }

    public void RemoveSponsorship(DateTimeOffset now)
    {
        SponsoredUntil = null;
        Touch(now);
    }

    public bool IsSponsored(DateTimeOffset now) => SponsoredUntil is not null && SponsoredUntil > now;

    /// <summary>
    /// Tipo da mídia pela extensão do nome do arquivo: <c>.mp4</c> e <c>.webm</c> são vídeos; os demais, fotos.
    /// </summary>
    public static PropertyMediaType GetMediaType(string fileName) =>
        _videoExtensions.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            ? PropertyMediaType.Video
            : PropertyMediaType.Photo;

    /// <summary>
    /// Adiciona uma foto ou um vídeo. Fotos entram no fim do carrossel; vídeos entram no começo, logo depois dos vídeos que já
    /// abrem o carrossel. A ordem pode ser mudada depois com <see cref="ReorderMedia"/>.
    /// </summary>
    public void AddMedia(string fileName, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > MediaFileNameMaxLength)
        {
            throw new DomainException("Nome de arquivo de foto ou vídeo inválido.");
        }

        if (_media.Count >= MaxMedia)
        {
            throw new DomainException($"Um imóvel pode ter no máximo {MaxMedia} fotos e vídeos.");
        }

        if (_media.Contains(fileName))
        {
            throw new DomainException("Este arquivo já foi adicionado ao imóvel.");
        }

        if (GetMediaType(fileName) == PropertyMediaType.Photo)
        {
            _media = [.. _media, fileName];
        }
        else
        {
            if (_media.Count(IsVideo) >= MaxVideos)
            {
                throw new DomainException($"Um imóvel pode ter no máximo {MaxVideos} vídeos.");
            }

            var position = _media.TakeWhile(IsVideo).Count();
            _media = [.. _media[..position], fileName, .. _media[position..]];
        }

        Touch(now);
    }

    public void RemoveMedia(string fileName, DateTimeOffset now)
    {
        if (!_media.Contains(fileName))
        {
            throw new DomainException("O arquivo não pertence a este imóvel.");
        }

        _media = [.. _media.Where(media => media != fileName)];
        Touch(now);
    }

    /// <summary>
    /// Reordena fotos e vídeos do carrossel. A lista deve conter exatamente as mídias atuais. A capa continua sendo a primeira foto.
    /// </summary>
    public void ReorderMedia(IReadOnlyList<string> fileNames, DateTimeOffset now)
    {
        if (fileNames.Count != _media.Count || fileNames.Distinct().Count() != fileNames.Count || fileNames.Except(_media).Any())
        {
            throw new DomainException("A nova ordem deve conter exatamente as fotos e vídeos atuais do imóvel.");
        }

        _media = [.. fileNames];
        Touch(now);
    }

    private static void EnsureStatusMatchesPurpose(PropertyStatus status, PropertyPurpose purpose)
    {
        if (status == PropertyStatus.Sold && purpose != PropertyPurpose.Sale)
        {
            throw new DomainException("Somente imóveis à venda podem ser marcados como vendidos.");
        }

        if (status == PropertyStatus.Rented && purpose != PropertyPurpose.Rent)
        {
            throw new DomainException("Somente imóveis para aluguel podem ser marcados como alugados.");
        }
    }

    private void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("O campo Título é obrigatório.");
        }

        if (title.Trim().Length > TitleMaxLength)
        {
            throw new DomainException($"O campo Título deve ter no máximo {TitleMaxLength} caracteres.");
        }

        Title = title.Trim();
    }

    private void SetDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("O campo Descrição é obrigatório.");
        }

        if (description.Trim().Length > DescriptionMaxLength)
        {
            throw new DomainException($"O campo Descrição deve ter no máximo {DescriptionMaxLength} caracteres.");
        }

        Description = description.Trim();
    }

    private void SetType(PropertyType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Tipo de imóvel inválido.");
        }

        Type = type;
    }

    private void SetPurpose(PropertyPurpose purpose)
    {
        if (!Enum.IsDefined(purpose))
        {
            throw new DomainException("Finalidade inválida.");
        }

        EnsureStatusMatchesPurpose(Status, purpose);
        Purpose = purpose;
    }

    private void SetPrice(decimal price)
    {
        if (price <= 0)
        {
            throw new DomainException("O valor do imóvel deve ser maior que zero.");
        }

        Price = price;
    }

    private void SetCondominiumFee(decimal? condominiumFee)
    {
        if (condominiumFee < 0)
        {
            throw new DomainException("O valor do condomínio não pode ser negativo.");
        }

        CondominiumFee = condominiumFee is 0 ? null : condominiumFee;
    }

    private void SetPropertyTax(decimal? propertyTax)
    {
        if (propertyTax < 0)
        {
            throw new DomainException("O valor do IPTU não pode ser negativo.");
        }

        PropertyTax = propertyTax is 0 ? null : propertyTax;
    }

    private void SetArea(decimal area)
    {
        if (area <= 0)
        {
            throw new DomainException("A metragem deve ser maior que zero.");
        }

        Area = area;
    }

    private void SetRooms(int bedrooms, int bathrooms, int parkingSpaces)
    {
        if (bedrooms is < 0 or > MaxRooms || bathrooms is < 0 or > MaxRooms || parkingSpaces is < 0 or > MaxRooms)
        {
            throw new DomainException($"Quartos, banheiros e vagas devem estar entre 0 e {MaxRooms}.");
        }

        if (Type == PropertyType.Land && (bedrooms > 0 || bathrooms > 0))
        {
            throw new DomainException("Terreno não pode ter quartos nem banheiros.");
        }

        Bedrooms = bedrooms;
        Bathrooms = bathrooms;
        ParkingSpaces = parkingSpaces;
    }

    private static bool IsVideo(string fileName) => GetMediaType(fileName) == PropertyMediaType.Video;

    private void Touch(DateTimeOffset now) => UpdatedAt = now.ToUniversalTime();
}
