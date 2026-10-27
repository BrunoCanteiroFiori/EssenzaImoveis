using System.ComponentModel.DataAnnotations;

namespace Backend.Application.Properties.Dtos;

/// <summary>
/// Nova ordem das imagens (nomes de arquivo). A primeira passa a ser a capa.
/// </summary>
public sealed record ReorderPropertyImagesRequest([Required, MinLength(1)] IReadOnlyList<string> FileNames);
