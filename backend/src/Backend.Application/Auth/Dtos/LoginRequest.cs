using System.ComponentModel.DataAnnotations;
using Backend.Domain.Common;
using Backend.Domain.Users;

namespace Backend.Application.Auth.Dtos;

public sealed record LoginRequest(
    [Required, MaxLength(EmailAddress.MaxLength)] string Email,
    [Required, MaxLength(PasswordPolicy.MaxLength)] string Password);
