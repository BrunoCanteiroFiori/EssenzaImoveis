namespace Backend.Infrastructure.Storage;

/// <summary>
/// Acesso ao Google Drive de uma conta pessoal por OAuth: cliente OAuth (<see cref="ClientId"/> e <see cref="ClientSecret"/>)
/// e um refresh token gerado uma vez com o escopo <c>drive.file</c>. <see cref="RootFolderName"/> é a pasta criada pelo
/// backend na raiz do Meu Drive. É classe, e não record, para o <c>ToString</c> não expor os segredos.
/// </summary>
public sealed class GoogleDriveOptions(string clientId, string clientSecret, string refreshToken, string rootFolderName)
{
    public string ClientId { get; } = clientId;

    public string ClientSecret { get; } = clientSecret;

    public string RefreshToken { get; } = refreshToken;

    public string RootFolderName { get; } = rootFolderName;
}
