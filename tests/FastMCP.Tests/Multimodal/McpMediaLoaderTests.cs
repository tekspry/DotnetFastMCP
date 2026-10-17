using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastMCP;
using FastMCP.Protocol;
using Xunit;

namespace FastMCP.Tests.Multimodal;

public class McpMediaLoaderTests
{
    private static readonly byte[] SamplePngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01 };
    private static readonly string SamplePngBase64 = Convert.ToBase64String(SamplePngBytes);

    // =========================================================================
    // 1. Data URI Tests (RFC 2397)
    // =========================================================================

    [Fact]
    public async Task LoadAsBytesAsync_WithBase64PngDataUri_ReturnsDecodedBytesAndPngMimeType()
    {
        var dataUri = $"data:image/png;base64,{SamplePngBase64}";

        var (bytes, mimeType) = await McpMediaLoader.LoadAsBytesAsync(dataUri);

        Assert.Equal("image/png", mimeType);
        Assert.Equal(SamplePngBytes, bytes);
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithBase64JpegDataUri_ReturnsJpegMimeType()
    {
        var sampleJpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        var base64 = Convert.ToBase64String(sampleJpegBytes);
        var dataUri = $"data:image/jpeg;base64,{base64}";

        var (bytes, mimeType) = await McpMediaLoader.LoadAsBytesAsync(dataUri);

        Assert.Equal("image/jpeg", mimeType);
        Assert.Equal(sampleJpegBytes, bytes);
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithUrlEncodedTextDataUri_ReturnsDecodedUtf8Bytes()
    {
        var dataUri = "data:text/plain;charset=utf-8,Hello%20FastMCP%20World%21";

        var (bytes, mimeType) = await McpMediaLoader.LoadAsBytesAsync(dataUri);

        Assert.Equal("text/plain", mimeType);
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Equal("Hello FastMCP World!", text);
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithDataUriContainingWhitespaceInBase64_CleansAndDecodesSuccessfully()
    {
        var base64WithWhitespace = $"  {SamplePngBase64.Substring(0, 4)}\r\n{SamplePngBase64.Substring(4)}  ";
        var dataUri = $"data:image/png;base64,{base64WithWhitespace}";

        var (bytes, mimeType) = await McpMediaLoader.LoadAsBytesAsync(dataUri);

        Assert.Equal("image/png", mimeType);
        Assert.Equal(SamplePngBytes, bytes);
    }

    [Fact]
    public async Task LoadAsBase64Async_WithBase64DataUri_ReturnsBase64StringDirectly()
    {
        var dataUri = $"data:image/webp;base64,{SamplePngBase64}";

        var (base64, mimeType) = await McpMediaLoader.LoadAsBase64Async(dataUri);

        Assert.Equal("image/webp", mimeType);
        Assert.Equal(SamplePngBase64, base64);
    }

    // =========================================================================
    // 2. Local File & file:// URI Tests
    // =========================================================================

    [Fact]
    public async Task LoadAsBytesAsync_WithLocalFilePath_ReturnsFileBytesAndSniffedMimeType()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"fastmcp_test_{Guid.NewGuid():N}.png");
        try
        {
            await File.WriteAllBytesAsync(tempFile, SamplePngBytes);

            var (bytes, mimeType) = await McpMediaLoader.LoadAsBytesAsync(tempFile);

            Assert.Equal("image/png", mimeType);
            Assert.Equal(SamplePngBytes, bytes);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithFileUriScheme_ReturnsFileBytes()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"fastmcp_test_{Guid.NewGuid():N}.webp");
        try
        {
            await File.WriteAllBytesAsync(tempFile, SamplePngBytes);
            var fileUri = new Uri(tempFile).AbsoluteUri; // e.g. file:///C:/...

            var (bytes, mimeType) = await McpMediaLoader.LoadAsBytesAsync(fileUri);

            Assert.Equal("image/webp", mimeType);
            Assert.Equal(SamplePngBytes, bytes);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithBaseDirectory_ResolvesRelativePath()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), $"fastmcp_base_{Guid.NewGuid():N}");
        var subDir = Path.Combine(baseDir, "uploads");
        Directory.CreateDirectory(subDir);
        var fileName = "photo.jpg";
        var fullPath = Path.Combine(subDir, fileName);

        try
        {
            await File.WriteAllBytesAsync(fullPath, SamplePngBytes);

            var (bytes, mimeType) = await McpMediaLoader.LoadAsBytesAsync(
                uri: "uploads/photo.jpg",
                baseDirectory: baseDir);

            Assert.Equal("image/jpeg", mimeType);
            Assert.Equal(SamplePngBytes, bytes);
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
        }
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithEmptyFile_ReturnsEmptyArrayAndValidMime()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"fastmcp_empty_{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllBytesAsync(tempFile, Array.Empty<byte>());

            var (bytes, mimeType) = await McpMediaLoader.LoadAsBytesAsync(tempFile);

            Assert.Equal("application/json", mimeType);
            Assert.Empty(bytes);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    // =========================================================================
    // 3. HTTP / HTTPS Tests (Mocked)
    // =========================================================================

    [Fact]
    public async Task LoadAsBytesAsync_WithHttpUrl_DownloadsBytesAndExtractsHeaderMime()
    {
        var testHandler = new MockHttpMessageHandler(
            expectedUri: "https://example.com/assets/banner",
            statusCode: HttpStatusCode.OK,
            responseBytes: SamplePngBytes,
            responseMediaType: "image/png");

        using var client = new HttpClient(testHandler);

        var (bytes, mimeType) = await McpMediaLoader.LoadAsBytesAsync("https://example.com/assets/banner", client);

        Assert.Equal("image/png", mimeType);
        Assert.Equal(SamplePngBytes, bytes);
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithHttpUrlMissingContentTypeHeader_SniffsMimeFromUrl()
    {
        var testHandler = new MockHttpMessageHandler(
            expectedUri: "https://example.com/assets/graphic.svg",
            statusCode: HttpStatusCode.OK,
            responseBytes: SamplePngBytes,
            responseMediaType: null); // No media type header

        using var client = new HttpClient(testHandler);

        var (bytes, mimeType) = await McpMediaLoader.LoadAsBytesAsync("https://example.com/assets/graphic.svg", client);

        Assert.Equal("image/svg+xml", mimeType);
        Assert.Equal(SamplePngBytes, bytes);
    }

    // =========================================================================
    // 4. ImageContent & EmbeddedResource Factory Tests
    // =========================================================================

    [Fact]
    public async Task LoadAsImageContentAsync_ReturnsPopulatedImageContentItem()
    {
        var dataUri = $"data:image/png;base64,{SamplePngBase64}";

        var imageContent = await McpMediaLoader.LoadAsImageContentAsync(dataUri);

        Assert.NotNull(imageContent);
        Assert.Equal("image", imageContent.Type);
        Assert.Equal(SamplePngBase64, imageContent.Data);
        Assert.Equal("image/png", imageContent.MimeType);
    }

    [Fact]
    public async Task LoadAsEmbeddedResourceAsync_ReturnsPopulatedEmbeddedResource()
    {
        var dataUri = $"data:application/pdf;base64,{SamplePngBase64}";
        var resourceUri = "resource://documents/report.pdf";

        var embeddedResource = await McpMediaLoader.LoadAsEmbeddedResourceAsync(dataUri, resourceUri);

        Assert.NotNull(embeddedResource);
        Assert.Equal("resource", embeddedResource.Type);
        Assert.Equal(resourceUri, embeddedResource.Resource.Uri);
        Assert.Equal(SamplePngBase64, embeddedResource.Resource.Blob);
        Assert.Equal("application/pdf", embeddedResource.Resource.MimeType);
    }

    // =========================================================================
    // 5. Negative & Edge Case Tests
    // =========================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoadAsBytesAsync_WithNullOrWhitespaceUri_ThrowsArgumentException(string? invalidUri)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => McpMediaLoader.LoadAsBytesAsync(invalidUri!));
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithNonExistentLocalFile_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"does_not_exist_{Guid.NewGuid():N}.png");

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => McpMediaLoader.LoadAsBytesAsync(nonExistentPath));
        Assert.Contains(nonExistentPath, ex.Message);
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithMalformedDataUri_ThrowsFormatException()
    {
        var malformedUri = "data:image/png;base64"; // Missing comma separator

        await Assert.ThrowsAsync<FormatException>(() => McpMediaLoader.LoadAsBytesAsync(malformedUri));
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithInvalidBase64InDataUri_ThrowsFormatException()
    {
        var badBase64Uri = "data:image/png;base64,???not_valid_base64!!!";

        await Assert.ThrowsAsync<FormatException>(() => McpMediaLoader.LoadAsBytesAsync(badBase64Uri));
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithHttp404NotFound_ThrowsHttpRequestException()
    {
        var testHandler = new MockHttpMessageHandler(
            expectedUri: "https://example.com/not-found.png",
            statusCode: HttpStatusCode.NotFound,
            responseBytes: Array.Empty<byte>());

        using var client = new HttpClient(testHandler);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            McpMediaLoader.LoadAsBytesAsync("https://example.com/not-found.png", client));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithHttp500ServerError_ThrowsHttpRequestException()
    {
        var testHandler = new MockHttpMessageHandler(
            expectedUri: "https://example.com/error.jpg",
            statusCode: HttpStatusCode.InternalServerError,
            responseBytes: Array.Empty<byte>());

        using var client = new HttpClient(testHandler);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            McpMediaLoader.LoadAsBytesAsync("https://example.com/error.jpg", client));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
    }

    [Fact]
    public async Task LoadAsBytesAsync_WithCancelledCancellationToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            McpMediaLoader.LoadAsBytesAsync("https://example.com/image.png", cancellationToken: cts.Token));
    }

    [Fact]
    public void TryParseDataUri_WithInvalidInputs_ReturnsFalse()
    {
        Assert.False(McpMediaLoader.TryParseDataUri(null!, out _, out _));
        Assert.False(McpMediaLoader.TryParseDataUri("", out _, out _));
        Assert.False(McpMediaLoader.TryParseDataUri("http://example.com/image.png", out _, out _));
        Assert.False(McpMediaLoader.TryParseDataUri("file:///photo.jpg", out _, out _));
        Assert.False(McpMediaLoader.TryParseDataUri("data:image/png", out _, out _)); // missing comma
        Assert.False(McpMediaLoader.TryParseDataUri("data:image/png;base64,invalid!base64", out _, out _));
    }

    [Theory]
    [InlineData("image.png", "image/png")]
    [InlineData("picture.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("banner.webp", "image/webp")]
    [InlineData("animation.gif", "image/gif")]
    [InlineData("icon.svg", "image/svg+xml")]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("data.json", "application/json")]
    [InlineData("readme.txt", "text/plain")]
    [InlineData("audio.mp3", "audio/mpeg")]
    [InlineData("sound.wav", "audio/wav")]
    [InlineData("photo.png?version=123#anchor", "image/png")]
    [InlineData("unknown.customxyz", "application/octet-stream")]
    [InlineData("", "application/octet-stream")]
    public void GetMimeTypeFromExtension_ReturnsExpectedMime(string input, string expectedMime)
    {
        var result = McpMediaLoader.GetMimeTypeFromExtension(input);
        Assert.Equal(expectedMime, result);
    }

    // =========================================================================
    // Mock HTTP Message Handler for Isolated Network Unit Testing
    // =========================================================================
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _expectedUri;
        private readonly HttpStatusCode _statusCode;
        private readonly byte[] _responseBytes;
        private readonly string? _responseMediaType;

        public MockHttpMessageHandler(
            string expectedUri,
            HttpStatusCode statusCode,
            byte[] responseBytes,
            string? responseMediaType = null)
        {
            _expectedUri = expectedUri;
            _statusCode = statusCode;
            _responseBytes = responseBytes;
            _responseMediaType = responseMediaType;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            response.Content = new ByteArrayContent(_responseBytes);

            if (!string.IsNullOrWhiteSpace(_responseMediaType))
            {
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(_responseMediaType);
            }

            return Task.FromResult(response);
        }
    }
}
