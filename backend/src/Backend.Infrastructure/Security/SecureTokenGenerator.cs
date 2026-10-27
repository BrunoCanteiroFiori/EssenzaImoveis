using System.Buffers.Text;
using System.Security.Cryptography;

namespace Backend.Infrastructure.Security;

public sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    public string Generate(int byteCount = 32) => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(byteCount));
}
