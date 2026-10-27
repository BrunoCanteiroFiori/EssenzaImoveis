namespace Backend.Infrastructure.Security;

public interface ISecureTokenGenerator
{
    /// <summary>
    /// Gera um token aleatório criptograficamente seguro, em Base64 URL, com <paramref name="byteCount"/> bytes de entropia.
    /// </summary>
    string Generate(int byteCount = 32);
}
