namespace Backend.Infrastructure.Security;

public sealed record JwtToken(string Value, DateTimeOffset ExpiresAt);
