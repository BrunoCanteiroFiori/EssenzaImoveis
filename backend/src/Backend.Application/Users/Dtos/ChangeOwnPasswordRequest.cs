using System.ComponentModel.DataAnnotations;
using Backend.Domain.Users;

namespace Backend.Application.Users.Dtos;

public sealed record ChangeOwnPasswordRequest(
    [Required, MaxLength(PasswordPolicy.MaxLength)] string CurrentPassword,
    [Required, MaxLength(PasswordPolicy.MaxLength)] string NewPassword);
