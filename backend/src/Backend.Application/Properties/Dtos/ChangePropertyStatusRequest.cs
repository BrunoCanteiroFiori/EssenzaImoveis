using Backend.Domain.Properties;

namespace Backend.Application.Properties.Dtos;

public sealed record ChangePropertyStatusRequest(PropertyStatus Status);
