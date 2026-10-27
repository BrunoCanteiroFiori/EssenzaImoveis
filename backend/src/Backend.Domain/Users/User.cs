using Backend.Domain.Common;

namespace Backend.Domain.Users;

/// <summary>
/// Usuário da área autenticada. Existe um único super admin (conta fixa), que não pode ser desativado
/// nem ter as permissões alteradas; os demais usuários são da equipe, com permissões definidas pelo super admin.
/// </summary>
public sealed class User : AggregateRoot
{
    public const int NameMaxLength = 100;
    public const int PasswordHashMaxLength = 200;
    public const int MaxFailedSignInAttempts = 5;
    public const UserPermissions AllPermissions =
        UserPermissions.ReadProperties | UserPermissions.WriteProperties | UserPermissions.DeleteProperties | UserPermissions.ManageMessages;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private User()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public UserPermissions Permissions { get; private set; }

    public bool IsActive { get; private set; }

    public int FailedSignInAttempts { get; private set; }

    public DateTimeOffset? LockoutEndsAt { get; private set; }

    public DateTimeOffset? LastSignInAt { get; private set; }

    public DateTimeOffset PasswordChangedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;

    public static User Create(string name, string email, string passwordHash, UserPermissions permissions, DateTimeOffset now)
    {
        var user = New(UserRole.Staff, name, email, passwordHash, now);
        user.SetPermissions(permissions);
        return user;
    }

    public static User CreateSuperAdmin(string name, string email, string passwordHash, DateTimeOffset now)
    {
        var user = New(UserRole.SuperAdmin, name, email, passwordHash, now);
        user.SetPermissions(AllPermissions);
        return user;
    }

    public void Update(string name, string email, DateTimeOffset now)
    {
        SetName(name);
        Email = EmailAddress.Normalize(email);
        Touch(now);
    }

    public void ChangePermissions(UserPermissions permissions, DateTimeOffset now)
    {
        EnsureIsNotSuperAdmin("As permissões do super admin não podem ser alteradas.");
        SetPermissions(permissions);
        Touch(now);
    }

    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        Touch(now);
    }

    public void Deactivate(DateTimeOffset now)
    {
        EnsureIsNotSuperAdmin("O super admin não pode ser desativado.");
        IsActive = false;
        Touch(now);
    }

    public void ChangePasswordHash(string passwordHash, DateTimeOffset now)
    {
        SetPasswordHash(passwordHash);
        PasswordChangedAt = now.ToUniversalTime();
        FailedSignInAttempts = 0;
        LockoutEndsAt = null;
        Touch(now);
    }

    public bool HasPermission(UserPermissions permission) => IsSuperAdmin || Permissions.HasFlag(permission);

    public bool IsLockedOut(DateTimeOffset now) => LockoutEndsAt is not null && LockoutEndsAt > now;

    public bool CanSignIn(DateTimeOffset now) => IsActive && !IsLockedOut(now);

    /// <summary>
    /// Registra uma senha errada. Ao atingir <see cref="MaxFailedSignInAttempts"/> tentativas, bloqueia o acesso por <see cref="LockoutDuration"/>.
    /// </summary>
    public void RegisterFailedSignIn(DateTimeOffset now)
    {
        FailedSignInAttempts++;
        if (FailedSignInAttempts >= MaxFailedSignInAttempts)
        {
            LockoutEndsAt = now.Add(LockoutDuration).ToUniversalTime();
            FailedSignInAttempts = 0;
        }
    }

    public void RegisterSuccessfulSignIn(DateTimeOffset now)
    {
        FailedSignInAttempts = 0;
        LockoutEndsAt = null;
        LastSignInAt = now.ToUniversalTime();
    }

    /// <summary>
    /// Uma sessão só continua válida se o usuário está ativo e a sessão foi criada depois da última troca de senha.
    /// </summary>
    public bool AcceptsSessionCreatedAt(DateTimeOffset sessionCreatedAt) => IsActive && sessionCreatedAt >= PasswordChangedAt;

    private static User New(UserRole role, string name, string email, string passwordHash, DateTimeOffset now)
    {
        var user = new User
        {
            Role = role,
            IsActive = true,
            CreatedAt = now.ToUniversalTime(),
            PasswordChangedAt = now.ToUniversalTime(),
        };

        user.SetName(name);
        user.Email = EmailAddress.Normalize(email);
        user.SetPasswordHash(passwordHash);
        user.Touch(now);
        return user;
    }

    private void EnsureIsNotSuperAdmin(string message)
    {
        if (IsSuperAdmin)
        {
            throw new DomainException(message);
        }
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("O campo Nome é obrigatório.");
        }

        if (name.Trim().Length > NameMaxLength)
        {
            throw new DomainException($"O campo Nome deve ter no máximo {NameMaxLength} caracteres.");
        }

        Name = name.Trim();
    }

    private void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || passwordHash.Length > PasswordHashMaxLength)
        {
            throw new DomainException("Hash de senha inválido.");
        }

        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Quem pode alterar ou remover imóveis também precisa poder vê-los, então leitura é incluída automaticamente.
    /// </summary>
    private void SetPermissions(UserPermissions permissions)
    {
        if ((permissions & ~AllPermissions) != 0)
        {
            throw new DomainException("Permissão inválida.");
        }

        if (permissions.HasFlag(UserPermissions.WriteProperties) || permissions.HasFlag(UserPermissions.DeleteProperties))
        {
            permissions |= UserPermissions.ReadProperties;
        }

        Permissions = permissions;
    }

    private void Touch(DateTimeOffset now) => UpdatedAt = now.ToUniversalTime();
}
