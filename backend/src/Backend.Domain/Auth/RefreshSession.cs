using Backend.Domain.Common;

namespace Backend.Domain.Auth;

/// <summary>
/// Sessão de renovação do login. Guarda só o hash do refresh token; cada renovação revoga a sessão e cria outra (rotação).
/// </summary>
public sealed class RefreshSession : AggregateRoot
{
    public const int TokenHashLength = 64;

    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    /// <summary>
    /// Tolerância para requisições simultâneas (ex.: duas abas) que usam o mesmo token logo após a rotação.
    /// </summary>
    public static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    private RefreshSession()
    {
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public static RefreshSession Create(Guid userId, string tokenHash, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("O campo UserId é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length != TokenHashLength)
        {
            throw new DomainException("Hash do token inválido.");
        }

        return new RefreshSession
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = now.ToUniversalTime(),
            ExpiresAt = now.Add(Lifetime).ToUniversalTime(),
        };
    }

    public bool IsActive(DateTimeOffset now) => !IsRevoked && ExpiresAt > now;

    /// <summary>
    /// Uso de um token já revogado depois da tolerância indica possível roubo do token.
    /// </summary>
    public bool IsSuspiciousReuse(DateTimeOffset now) => RevokedAt is not null && now - RevokedAt > ReuseGracePeriod;

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now.ToUniversalTime();
    }
}
