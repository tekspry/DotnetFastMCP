using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastMCP;
using FastMCP.Protocol;
using Xunit;

namespace FastMCP.Tests.Multimodal;

public class McpContentTests
{
    private static readonly byte[] SamplePngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly string SamplePngBase64 = Convert.ToBase64String(SamplePngBytes);

    // =========================================================================
    // 1. Text & Json Content Tests
    // =========================================================================

    [Fact]
    public void Text_WithValidText_ReturnsPopulatedTextContent()
    {
        var content = McpContent.Text("Hello MCP");

        Assert.NotNull(content);
        Assert.Equal("text", content.Type);
        Assert.Equal("Hello MCP", content.Text);
    }

    [Fact]
    public void Text_WithNullText_ReturnsEmptyTextContent()
    {
        var content = McpContent.Text(null!);

        Assert.NotNull(content);
        Assert.Equal("text", content.Type);
        Assert.Equal(string.Empty, content.Text);
    }

    [Fact]
    public void Json_WithObject_SerializesToJsonString()
    {
        var model = new { Id = 42, Name = "Fashion Belt", InStock = true };

        var content = McpContent.Json(model);

        Assert.NotNull(content);
        Assert.Equal("text", content.Type);
        Assert.Contains("\"Id\":42", content.Text);
        Assert.Contains("\"Name\":\"Fashion Belt\"", content.Text);
    }

    [Fact]
    public void Json_WithNull_ReturnsLiteralNullText()
    {
        var content = McpContent.Json(null);

        Assert.NotNull(content);
        Assert.Equal("null", content.Text);
    }

    [Fact]
    public void Json_WithString_ReturnsRawStringWithoutDoubleQuoting()
    {
        var rawJson = "{\"status\":\"ok\"}";

        var content = McpContent.Json(rawJson);

        Assert.Equal(rawJson, content.Text);
    }

    [Fact]
    public void Json_WithCustomOptions_HonorsOptions()
    {
        var model = new { FirstName = "Alice" };
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var content = McpContent.Json(model, options);

        Assert.Contains("\"firstName\":\"Alice\"", content.Text);
    }

    // =========================================================================
    // 2. Image Content Tests
    // =========================================================================

    [Fact]
    public void Image_WithBase64AndMime_ReturnsImageContent()
    {
        var content = McpContent.Image(SamplePngBase64, "image/png");

        Assert.NotNull(content);
        Assert.Equal("image", content.Type);
        Assert.Equal(SamplePngBase64, content.Data);
        Assert.Equal("image/png", content.MimeType);
    }

    [Fact]
    public void ImageFromBytes_WithByteArray_EncodesToBase64()
    {
        var content = McpContent.ImageFromBytes(SamplePngBytes, "image/webp");

        Assert.NotNull(content);
        Assert.Equal("image", content.Type);
        Assert.Equal(SamplePngBase64, content.Data);
        Assert.Equal("image/webp", content.MimeType);
    }

    [Fact]
    public void ImageFromBytes_WithEmptyOrNullArray_ReturnsEmptyBase64String()
    {
        var contentEmpty = McpContent.ImageFromBytes(Array.Empty<byte>());
        Assert.Equal(string.Empty, contentEmpty.Data);

        var contentNull = McpContent.ImageFromBytes(null!);
        Assert.Equal(string.Empty, contentNull.Data);
    }

    [Fact]
    public async Task ImageFromFileAsync_WithValidFile_LoadsAndEncodes()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"mcpcontent_test_{Guid.NewGuid():N}.png");
        try
        {
            await File.WriteAllBytesAsync(tempFile, SamplePngBytes);

            var content = await McpContent.ImageFromFileAsync(tempFile, "image/png");

            Assert.Equal(SamplePngBase64, content.Data);
            Assert.Equal("image/png", content.MimeType);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ImageFromStreamAsync_WithMemoryStream_ReadsAndEncodes()
    {
        using var stream = new MemoryStream(SamplePngBytes);

        var content = await McpContent.ImageFromStreamAsync(stream, "image/jpeg");

        Assert.NotNull(content);
        Assert.Equal("image", content.Type);
        Assert.Equal(SamplePngBase64, content.Data);
        Assert.Equal("image/jpeg", content.MimeType);
    }

    [Fact]
    public async Task ImageFromStreamAsync_WithNullStream_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => McpContent.ImageFromStreamAsync(null!));
    }

    [Fact]
    public async Task ImageFromStreamAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        using var stream = new MemoryStream(SamplePngBytes);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            McpContent.ImageFromStreamAsync(stream, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ImageFromUriAsync_WithDataUri_ReturnsImageContent()
    {
        var dataUri = $"data:image/png;base64,{SamplePngBase64}";

        var content = await McpContent.ImageFromUriAsync(dataUri);

        Assert.NotNull(content);
        Assert.Equal("image", content.Type);
        Assert.Equal(SamplePngBase64, content.Data);
        Assert.Equal("image/png", content.MimeType);
    }

    // =========================================================================
    // 3. Embedded Resource Tests
    // =========================================================================

    [Fact]
    public void ResourceText_WithTextAndUri_ReturnsEmbeddedResource()
    {
        var uri = "resource://prompts/system.md";
        var text = "You are a fashion AI assistant.";

        var resource = McpContent.ResourceText(uri, text, "text/markdown");

        Assert.NotNull(resource);
        Assert.Equal("resource", resource.Type);
        Assert.Equal(uri, resource.Resource.Uri);
        Assert.Equal(text, resource.Resource.Text);
        Assert.Equal("text/markdown", resource.Resource.MimeType);
        Assert.Null(resource.Resource.Blob);
    }

    [Fact]
    public void ResourceBlob_WithBytes_EncodesBlobToBase64()
    {
        var uri = "resource://blobs/artifact.bin";

        var resource = McpContent.ResourceBlob(uri, SamplePngBytes, "application/octet-stream");

        Assert.NotNull(resource);
        Assert.Equal("resource", resource.Type);
        Assert.Equal(uri, resource.Resource.Uri);
        Assert.Equal(SamplePngBase64, resource.Resource.Blob);
        Assert.Equal("application/octet-stream", resource.Resource.MimeType);
        Assert.Null(resource.Resource.Text);
    }

    [Fact]
    public void ResourceBlob_WithBase64String_SetsBlobDirectly()
    {
        var uri = "resource://blobs/logo.png";

        var resource = McpContent.ResourceBlob(uri, SamplePngBase64, "image/png");

        Assert.NotNull(resource);
        Assert.Equal(SamplePngBase64, resource.Resource.Blob);
        Assert.Equal("image/png", resource.Resource.MimeType);
    }

    // =========================================================================
    // 4. Composite List Tests
    // =========================================================================

    [Fact]
    public void Composite_WithMultipleItems_ReturnsListContainingAllItems()
    {
        var text = McpContent.Text("Generated image below:");
        var image = McpContent.Image(SamplePngBase64, "image/png");

        var composite = McpContent.Composite(text, image);

        Assert.NotNull(composite);
        Assert.Equal(2, composite.Count);
        Assert.IsType<TextContent>(composite[0]);
        Assert.IsType<ImageContent>(composite[1]);
    }

    [Fact]
    public void Composite_WithNullOrEmpty_ReturnsEmptyList()
    {
        var emptyParams = McpContent.Composite();
        Assert.NotNull(emptyParams);
        Assert.Empty(emptyParams);

        var nullParams = McpContent.Composite((ContentItem[])null!);
        Assert.NotNull(nullParams);
        Assert.Empty(nullParams);
    }
}
