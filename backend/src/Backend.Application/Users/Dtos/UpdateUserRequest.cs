using System.ComponentModel.DataAnnotations;
using Backend.Domain.Common;
using Backend.Domain.Users;

namespace Backend.Application.Users.Dtos;

/// <summary>
/// Atualiza nome, e-mail e permissões. As permissões do super admin são ignoradas (ele sempre tem todas).
/// </summary>
public sealed record UpdateUserRequest(
    [Required, MaxLength(User.NameMaxLength)] string Name,
    [Required, MaxLength(EmailAddress.MaxLength)] string Email,
    [Required] IReadOnlyList<string> Permissions);
