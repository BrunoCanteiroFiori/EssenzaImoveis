using Backend.Domain.Properties;

namespace Backend.Application.Properties.Dtos;

public sealed record TemperatureResponse(int RecentViews, TemperatureLevel Level);
