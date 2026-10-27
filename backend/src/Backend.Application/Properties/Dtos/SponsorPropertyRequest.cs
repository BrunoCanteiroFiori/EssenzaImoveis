using System.ComponentModel.DataAnnotations;

namespace Backend.Application.Properties.Dtos;

/// <summary>
/// Patrocina o imóvel por <see cref="Days"/> dias a partir de agora.
/// </summary>
public sealed record SponsorPropertyRequest([Range(1, 365)] int Days);
