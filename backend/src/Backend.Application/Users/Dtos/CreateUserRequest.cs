using System.ComponentModel.DataAnnotations;
using Backend.Domain.Common;
using Backend.Domain.Users;

namespace Backend.Application.Users.Dtos;

public sealed record CreateUserRequest(
    [Required, MaxLength(User.NameMaxLength)] string Name,
    [Required, MaxLength(EmailAddress.MaxLength)] string Email,
    [Required, MaxLength(PasswordPolicy.MaxLength)] string Password,
    [Required] IReadOnlyList<string> Permissions);
