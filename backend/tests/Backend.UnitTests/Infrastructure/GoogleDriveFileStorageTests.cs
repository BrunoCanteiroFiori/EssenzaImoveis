using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Backend.Infrastructure.Storage;
using Google.Apis.Drive.v3;
using Google.Apis.Http;
using Google.Apis.Services;

namespace Backend.UnitTests.Infrastructure;

public sealed partial class GoogleDriveFileStorageTests : IDisposable
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";

    private readonly FakeDriveHandler _drive = new();
    private readonly DriveService _driveService;
    private readonly GoogleDriveFileStorage _fileStorage;

    public GoogleDriveFileStorageTests()
    {
        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientFactory = new FakeHttpClientFactory(_drive),
            GZipEnabled = false,
        });
        _fileStorage = new GoogleDriveFileStorage(_driveService, new GoogleDriveOptions("client-id", "client-secret", "refresh-token", "Imoveis"));
    }

    [Fact]
    public async Task SaveAsync_FirstFile_ShouldCreatePublicRootFolderSubfoldersAndUpload()
    {
        using var content = new MemoryStream([1, 2, 3]);

        await _fileStorage.SaveAsync("properties/abc/foto.png", content, "image/png");

        var root = Assert.Single(_drive.Items, item => item.Name == "Imoveis");
        var properties = Assert.Single(_drive.Items, item => item.Name == "properties");
        var propertyFolder = Assert.Single(_drive.Items, item => item.Name == "abc");
        var file = Assert.Single(_drive.Items, item => item.Name == "foto.png");
        Assert.Equal("root", root.ParentId);
        Assert.Equal(root.Id, properties.ParentId);
        Assert.Equal(properties.Id, propertyFolder.ParentId);
        Assert.Equal(propertyFolder.Id, file.ParentId);
        Assert.Equal("image/png", file.MimeType);
        Assert.Equal([1, 2, 3], file.Content);
        Assert.Equal([root.Id], _drive.PublicIds);
    }

    [Fact]
    public async Task SaveAsync_SecondFileInSameFolder_ShouldReuseFoldersWithoutSearchingAgain()
    {
        using var first = new MemoryStream([1]);
        using var second = new MemoryStream([2]);
        await _fileStorage.SaveAsync("properties/abc/a.png", first, "image/png");
        var searches = _drive.SearchCount;

        await _fileStorage.SaveAsync("properties/abc/b.png", second, "image/png");

        Assert.Equal(3, _drive.Items.Count(item => item.MimeType == FolderMimeType));
        Assert.Equal(searches, _drive.SearchCount);
    }

    [Fact]
    public async Task OpenReadAsync_WithExistingFile_ShouldFindItInDriveAndReadContent()
    {
        using var content = new MemoryStream([1, 2, 3, 4, 5]);
        await _fileStorage.SaveAsync("properties/abc/video.mp4", content, "video/mp4");
        using var otherStorage = new GoogleDriveFileStorage(_driveService, new GoogleDriveOptions("client-id", "client-secret", "refresh-token", "Imoveis"));

        var file = await otherStorage.OpenReadAsync("properties/abc/video.mp4");

        Assert.NotNull(file);
        await using var stream = file.Content;
        using var destination = new MemoryStream();
        stream.Seek(2, SeekOrigin.Begin);
        await stream.CopyToAsync(destination);
        Assert.Equal("video/mp4", file.ContentType);
        Assert.Equal(5, stream.Length);
        Assert.Equal([3, 4, 5], destination.ToArray());
    }

    [Fact]
    public async Task OpenReadAsync_WithMissingFile_ShouldReturnNullWithoutCreatingFolders()
    {
        var file = await _fileStorage.OpenReadAsync("properties/abc/inexistente.png");

        Assert.Null(file);
        Assert.Empty(_drive.Items);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingFile_ShouldMoveItToTrash()
    {
        using var content = new MemoryStream([1]);
        await _fileStorage.SaveAsync("properties/abc/foto.png", content, "image/png");

        await _fileStorage.DeleteAsync("properties/abc/foto.png");

        Assert.True(Assert.Single(_drive.Items, item => item.Name == "foto.png").Trashed);
        Assert.Null(await _fileStorage.OpenReadAsync("properties/abc/foto.png"));
    }

    [Fact]
    public async Task DeleteDirectoryAsync_ShouldMoveFolderToTrashAndForgetItsFiles()
    {
        using var content = new MemoryStream([1]);
        await _fileStorage.SaveAsync("properties/abc/foto.png", content, "image/png");

        await _fileStorage.DeleteDirectoryAsync("properties/abc");

        Assert.True(Assert.Single(_drive.Items, item => item.Name == "abc").Trashed);
        Assert.Null(await _fileStorage.OpenReadAsync("properties/abc/foto.png"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("properties/../foto.png")]
    public async Task SaveAsync_WithInvalidPath_ShouldThrowArgumentException(string relativePath)
    {
        using var content = new MemoryStream([1]);

        await Assert.ThrowsAsync<ArgumentException>(() => _fileStorage.SaveAsync(relativePath, content, "image/png"));
    }

    public void Dispose()
    {
        _fileStorage.Dispose();
        _driveService.Dispose();
    }

    [GeneratedRegex(@"^name = '(?<name>[^']*)' and '(?<parent>[^']*)' in parents and trashed = false and mimeType (?<operator>!?=) ")]
    private static partial Regex SearchQueryRegex();

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : HttpClientFactory
    {
        protected override HttpMessageHandler CreateHandler(CreateHttpClientArgs args) => handler;
    }

    private sealed class FakeDriveItem(string id, string name, string parentId, string mimeType)
    {
        public string Id { get; } = id;

        public string Name { get; } = name;

        public string ParentId { get; } = parentId;

        public string MimeType { get; } = mimeType;

        public byte[] Content { get; set; } = [];

        public bool Trashed { get; set; }
    }

    /// <summary>
    /// Drive em memória com só o que o armazenamento usa: busca, criação de pasta, permissão, upload resumable,
    /// lixeira e download com Range.
    /// </summary>
    private sealed class FakeDriveHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, FakeDriveItem> _pendingUploads = [];
        private int _nextId;

        public List<FakeDriveItem> Items { get; } = [];

        public List<string> PublicIds { get; } = [];

        public int SearchCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            var body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);

            return (request.Method.Method, path) switch
            {
                ("GET", "/drive/v3/files") => Search(query["q"]!),
                ("POST", "/drive/v3/files") => Json(CreateItem(body!, FolderMimeType)),
                ("POST", _) when path.EndsWith("/permissions", StringComparison.Ordinal) => Json(AddPermission(path)),
                ("POST", "/upload/drive/v3/files") => StartUpload(body!, request.Headers.GetValues("X-Upload-Content-Type").Single()),
                ("PUT", "/upload/drive/v3/files") => Json(FinishUpload(query["upload_id"]!, body!)),
                ("PATCH", _) => Json(Trash(path)),
                ("GET", _) when query["alt"] == "media" => Download(path, request),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            };
        }

        private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json"),
        };

        private static object ToJson(FakeDriveItem item) =>
            new { id = item.Id, mimeType = item.MimeType, size = item.Content.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) };

        private HttpResponseMessage Search(string searchQuery)
        {
            SearchCount++;
            var match = SearchQueryRegex().Match(searchQuery);
            var isFolder = match.Groups["operator"].Value == "=";
            var files = Items
                .Where(item => item.Name == match.Groups["name"].Value && item.ParentId == match.Groups["parent"].Value)
                .Where(item => !item.Trashed && (item.MimeType == FolderMimeType) == isFolder)
                .Select(ToJson);
            return Json(new { files });
        }

        private object CreateItem(byte[] body, string mimeType)
        {
            using var metadata = JsonDocument.Parse(body);
            var item = new FakeDriveItem(
                $"id-{++_nextId}",
                metadata.RootElement.GetProperty("name").GetString()!,
                metadata.RootElement.GetProperty("parents")[0].GetString()!,
                metadata.RootElement.TryGetProperty("mimeType", out var type) ? type.GetString()! : mimeType);
            Items.Add(item);
            return ToJson(item);
        }

        private object AddPermission(string path)
        {
            PublicIds.Add(path.Split('/')[^2]);
            return new { id = "permission" };
        }

        private HttpResponseMessage StartUpload(byte[] body, string contentType)
        {
            using var metadata = JsonDocument.Parse(body);
            var uploadId = $"upload-{++_nextId}";
            _pendingUploads[uploadId] = new FakeDriveItem(
                $"id-{_nextId}",
                metadata.RootElement.GetProperty("name").GetString()!,
                metadata.RootElement.GetProperty("parents")[0].GetString()!,
                contentType);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Location = new Uri($"https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable&upload_id={uploadId}");
            return response;
        }

        private object FinishUpload(string uploadId, byte[] content)
        {
            var item = _pendingUploads[uploadId];
            item.Content = content;
            Items.Add(item);
            return ToJson(item);
        }

        private object Trash(string path)
        {
            var item = Items.Single(item => item.Id == path.Split('/')[^1]);
            item.Trashed = true;
            return ToJson(item);
        }

        private HttpResponseMessage Download(string path, HttpRequestMessage request)
        {
            var item = Items.Single(item => item.Id == path.Split('/')[^1]);
            var from = (int)(request.Headers.Range?.Ranges.Single().From ?? 0);
            return new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent(item.Content[from..]) };
        }
    }
}
