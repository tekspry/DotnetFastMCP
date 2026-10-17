using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastMCP.Protocol;

namespace FastMCP;

/// <summary>
/// Fluent factory and helper methods for constructing spec-compliant Model Context Protocol (MCP) content items.
/// Supports text, serialized JSON, images from Base64/files/streams/bytes/URIs, embedded resources, and composite lists.
/// </summary>
public static class McpContent
{
    /// <summary>
    /// Creates a plain <see cref="TextContent"/> item.
    /// </summary>
    public static TextContent Text(string text) 
        => new TextContent { Text = text ?? string.Empty };

    /// <summary>
    /// Serializes an object or data structure into a JSON-formatted <see cref="TextContent"/> item.
    /// </summary>
    public static TextContent Json(object? data, JsonSerializerOptions? options = null)
    {
        if (data is null)
        {
            return new TextContent { Text = "null" };
        }

        if (data is string str)
        {
            return new TextContent { Text = str };
        }

        var json = JsonSerializer.Serialize(data, options);
        return new TextContent { Text = json };
    }

    /// <summary>
    /// Creates an <see cref="ImageContent"/> item from pre-encoded Base64 data.
    /// </summary>
    public static ImageContent Image(string base64Data, string mimeType)
        => new ImageContent { Data = base64Data ?? string.Empty, MimeType = mimeType ?? "image/png" };

    /// <summary>
    /// Creates an <see cref="ImageContent"/> item by Base64-encoding raw binary bytes.
    /// </summary>
    public static ImageContent ImageFromBytes(byte[] bytes, string mimeType = "image/png")
    {
        var base64 = bytes is not null && bytes.Length > 0 ? Convert.ToBase64String(bytes) : string.Empty;
        return new ImageContent { Data = base64, MimeType = mimeType ?? "image/png" };
    }

    /// <summary>
    /// Asynchronously loads an image from a local file path and returns an <see cref="ImageContent"/> item.
    /// </summary>
    public static async Task<ImageContent> ImageFromFileAsync(string path, string mimeType)
    {
        var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
        var base64 = Convert.ToBase64String(bytes);
        return new ImageContent { Data = base64, MimeType = mimeType };
    }

    /// <summary>
    /// Asynchronously reads an image stream to completion, encodes it to Base64, and returns an <see cref="ImageContent"/> item.
    /// </summary>
    public static async Task<ImageContent> ImageFromStreamAsync(
        Stream stream,
        string mimeType = "image/png",
        CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        cancellationToken.ThrowIfCancellationRequested();

        byte[] bytes;
        if (stream is MemoryStream ms)
        {
            bytes = ms.ToArray();
        }
        else
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            bytes = memoryStream.ToArray();
        }

        return ImageFromBytes(bytes, mimeType);
    }

    /// <summary>
    /// Resolves an image from any supported URI source (HTTP/HTTPS, local file, file://, or data: URI)
    /// using <see cref="McpMediaLoader"/> and returns an <see cref="ImageContent"/> item.
    /// </summary>
    public static Task<ImageContent> ImageFromUriAsync(
        string uri,
        HttpClient? httpClient = null,
        string? baseDirectory = null,
        CancellationToken cancellationToken = default)
        => McpMediaLoader.LoadAsImageContentAsync(uri, httpClient, baseDirectory, cancellationToken);

    /// <summary>
    /// Creates an <see cref="EmbeddedResource"/> item containing text content.
    /// </summary>
    public static EmbeddedResource ResourceText(string uri, string text, string? mimeType = "text/plain")
        => new EmbeddedResource
        {
            Resource = new ResourceContents
            {
                Uri = uri ?? string.Empty,
                Text = text ?? string.Empty,
                MimeType = mimeType
            }
        };

    /// <summary>
    /// Creates an <see cref="EmbeddedResource"/> item containing Base64 binary blob content from raw bytes.
    /// </summary>
    public static EmbeddedResource ResourceBlob(string uri, byte[] bytes, string? mimeType = "application/octet-stream")
    {
        var blob = bytes is not null && bytes.Length > 0 ? Convert.ToBase64String(bytes) : string.Empty;
        return new EmbeddedResource
        {
            Resource = new ResourceContents
            {
                Uri = uri ?? string.Empty,
                Blob = blob,
                MimeType = mimeType
            }
        };
    }

    /// <summary>
    /// Creates an <see cref="EmbeddedResource"/> item containing Base64 binary blob content.
    /// </summary>
    public static EmbeddedResource ResourceBlob(string uri, string base64Blob, string? mimeType = "application/octet-stream")
        => new EmbeddedResource
        {
            Resource = new ResourceContents
            {
                Uri = uri ?? string.Empty,
                Blob = base64Blob ?? string.Empty,
                MimeType = mimeType
            }
        };

    /// <summary>
    /// Combines multiple <see cref="ContentItem"/> elements into a composite list for mixed multimodal responses.
    /// </summary>
    public static List<ContentItem> Composite(params ContentItem[] items)
        => items is not null ? new List<ContentItem>(items) : new List<ContentItem>();

    /// <summary>
    /// Combines a sequence of <see cref="ContentItem"/> elements into a composite list for mixed multimodal responses.
    /// </summary>
    public static List<ContentItem> Composite(IEnumerable<ContentItem> items)
        => items is not null ? new List<ContentItem>(items) : new List<ContentItem>();
}