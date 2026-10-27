using System.ComponentModel.DataAnnotations;

namespace Backend.Application.PropertyViews.Dtos;

/// <summary>
/// Identificador anônimo do visitante gerado pelo navegador. Só o hash dele é gravado.
/// </summary>
public sealed record RegisterPropertyViewRequest([Required, MinLength(8), MaxLength(100)] string VisitorId);
