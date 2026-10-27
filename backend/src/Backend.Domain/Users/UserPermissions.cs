namespace Backend.Domain.Users;

[Flags]
public enum UserPermissions
{
    None = 0,
    ReadProperties = 1,
    WriteProperties = 2,
    DeleteProperties = 4,
    ManageMessages = 8,
}
