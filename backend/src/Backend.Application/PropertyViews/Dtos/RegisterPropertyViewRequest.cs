using System.ComponentModel.DataAnnotations;
using Backend.Application.Common;

namespace Backend.Application.PropertyViews.Dtos;

/// <summary>
/// Identificador anônimo do visitante gerado pelo navegador. Só o hash dele é gravado.
/// </summary>
public sealed record RegisterPropertyViewRequest(
    [Required, MinLength(VisitorIdentity.IdMinLength), MaxLength(VisitorIdentity.IdMaxLength)] string VisitorId);
