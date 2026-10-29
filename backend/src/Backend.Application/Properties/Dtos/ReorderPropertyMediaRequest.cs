using System.ComponentModel.DataAnnotations;

namespace Backend.Application.Properties.Dtos;

/// <summary>
/// Nova ordem das fotos e vídeos do carrossel (nomes de arquivo). A capa é a primeira foto da lista.
/// </summary>
public sealed record ReorderPropertyMediaRequest([Required, MinLength(1)] IReadOnlyList<string> FileNames);
