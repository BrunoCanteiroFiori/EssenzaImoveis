using Backend.Application.Users.Dtos;
using Backend.Domain.Users;

namespace Backend.Application.Users;

public static class UserMappings
{
    public static UserResponse ToResponse(this User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Role,
        user.Permissions.ToNames(),
        user.IsActive,
        user.LastSignInAt,
        user.CreatedAt,
        user.UpdatedAt);
}
