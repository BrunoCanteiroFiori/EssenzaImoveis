using System.ComponentModel.DataAnnotations;
using Backend.Domain.Users;

namespace Backend.Application.Users.Dtos;

public sealed record ResetUserPasswordRequest([Required, MaxLength(PasswordPolicy.MaxLength)] string NewPassword);
