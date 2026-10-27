namespace Backend.Domain.Abstractions;

/// <summary>
/// Usuário autenticado que está executando a operação atual (nulo para visitantes e processos internos).
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }
}
