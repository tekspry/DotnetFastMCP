using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastMCP.Protocol;

namespace FastMCP;

/// <summary>
/// High-performance, resilient media resolver for Model Context Protocol (MCP) servers.
/// Unifies media resolution across HTTP/HTTPS URLs, local file paths, file:// URIs, and Base64 data: URIs.
/// </summary>
public static class McpMediaLoader
{
    private static readonly HttpClient _defaultHttpClient = new HttpClient();

    private static readonly Dictionary<string, string> _extensionToMime = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        { ".png", "image/png" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".webp", "image/webp" },
        { ".gif", "image/gif" },
        { ".svg", "image/svg+xml" },
        { ".bmp", "image/bmp" },
        { ".ico", "image/x-icon" },
        { ".tiff", "image/tiff" },
        { ".tif", "image/tiff" },

        // Audio
        { ".mp3", "audio/mpeg" },
        { ".wav", "audio/wav" },
        { ".ogg", "audio/ogg" },
        { ".m4a", "audio/mp4" },
        { ".flac", "audio/flac" },

        // Documents & Structured Data
        { ".pdf", "application/pdf" },
        { ".json", "application/json" },
        { ".xml", "application/xml" },
        { ".csv", "text/csv" },
        { ".txt", "text/plain" },
        { ".html", "text/html" },
        { ".htm", "text/html" },
        { ".md", "text/markdown" },
        { ".zip", "application/zip" },
        { ".bin", "application/octet-stream" }
    };

    /// <summary>
    /// Resolves media from any supported URI source (HTTP/HTTPS, local file, file://, or data: URI)
    /// and returns the decoded binary bytes and associated MIME type.
    /// </summary>
    /// <param name="uri">The target URI, local path, file:// URI, or data: URI to resolve.</param>
    /// <param name="httpClient">Optional HttpClient to use for remote HTTP downloads. If null, a shared client is used.</param>
    /// <param name="baseDirectory">Optional base directory for resolving relative paths.</param>
    /// <param name="cancellationToken">Cancellation token to cancel asynchronous I/O.</param>
    /// <returns>A tuple containing the raw media bytes and the detected MIME type.</returns>
    public static async Task<(byte[] Bytes, string MimeType)> LoadAsBytesAsync(
        string uri,
        HttpClient? httpClient = null,
        string? baseDirectory = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("Media URI cannot be null, empty, or whitespace.", nameof(uri));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 1. Data URI scheme (RFC 2397)
        if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            if (TryParseDataUri(uri, out var bytes, out var mimeType))
            {
                return (bytes, mimeType);
            }
            throw new FormatException($"Malformed data URI format: '{uri.Substring(0, Math.Min(uri.Length, 60))}...'");
        }

        // 2. HTTP / HTTPS URL
        if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var client = httpClient ?? _defaultHttpClient;
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Failed to download media from '{uri}'. Remote server responded with status code {response.StatusCode} ({(int)response.StatusCode}).",
                    null,
                    response.StatusCode);
            }

            var downloadedBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var mime = response.Content.Headers.ContentType?.MediaType;

            if (string.IsNullOrWhiteSpace(mime) || mime.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            {
                // Fall back to sniffing from URL extension
                mime = GetMimeTypeFromExtension(uri);
            }

            return (downloadedBytes, mime);
        }

        // 3. Local file or file:// URI
        string localPath;
        if (uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri) && parsedUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                localPath = parsedUri.LocalPath;
            }
            else
            {
                localPath = uri.Replace("file:///", "").Replace("file://", "");
            }
        }
        else
        {
            localPath = uri;
        }

        // Check if file exists directly
        if (File.Exists(localPath))
        {
            var fileBytes = await File.ReadAllBytesAsync(localPath, cancellationToken).ConfigureAwait(false);
            var mime = GetMimeTypeFromExtension(localPath);
            return (fileBytes, mime);
        }

        // If baseDirectory is provided, try resolving relative to it
        if (!string.IsNullOrWhiteSpace(baseDirectory))
        {
            var combinedPath = Path.Combine(baseDirectory, localPath.TrimStart('/', '\\'));
            if (File.Exists(combinedPath))
            {
                var fileBytes = await File.ReadAllBytesAsync(combinedPath, cancellationToken).ConfigureAwait(false);
                var mime = GetMimeTypeFromExtension(combinedPath);
                return (fileBytes, mime);
            }
        }

        throw new FileNotFoundException($"Media file not found at '{localPath}'.", localPath);
    }

    /// <summary>
    /// Resolves media from any supported URI source and returns the Base64-encoded string and MIME type.
    /// Optimized for data: URIs to avoid redundant byte[] conversions when base64 is already present.
    /// </summary>
    public static async Task<(string Base64, string MimeType)> LoadAsBase64Async(
        string uri,
        HttpClient? httpClient = null,
        string? baseDirectory = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("Media URI cannot be null, empty, or whitespace.", nameof(uri));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Optimized path for Base64 Data URIs
        if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = uri.IndexOf(',');
            if (commaIndex > 5)
            {
                var metadata = uri.Substring(5, commaIndex - 5);
                var isBase64 = metadata.IndexOf(";base64", StringComparison.OrdinalIgnoreCase) >= 0;
                var mimeType = metadata.Split(';')[0].Trim();
                if (string.IsNullOrWhiteSpace(mimeType))
                {
                    mimeType = "text/plain";
                }

                if (isBase64)
                {
                    var rawBase64 = uri.Substring(commaIndex + 1).Trim();
                    // Validate base64 validity by checking Convert.TryFromBase64String or length
                    if (!string.IsNullOrEmpty(rawBase64))
                    {
                        return (rawBase64, mimeType);
                    }
                }
            }
        }

        var (bytes, mime) = await LoadAsBytesAsync(uri, httpClient, baseDirectory, cancellationToken).ConfigureAwait(false);
        return (Convert.ToBase64String(bytes), mime);
    }

    /// <summary>
    /// Resolves media from any supported URI source directly into an MCP <see cref="ImageContent"/> item.
    /// </summary>
    public static async Task<ImageContent> LoadAsImageContentAsync(
        string uri,
        HttpClient? httpClient = null,
        string? baseDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var (base64, mimeType) = await LoadAsBase64Async(uri, httpClient, baseDirectory, cancellationToken).ConfigureAwait(false);
        return new ImageContent
        {
            Data = base64,
            MimeType = mimeType
        };
    }

    /// <summary>
    /// Resolves media from any supported URI source directly into an MCP <see cref="EmbeddedResource"/> item.
    /// </summary>
    public static async Task<EmbeddedResource> LoadAsEmbeddedResourceAsync(
        string mediaUri,
        string embeddedResourceUri,
        HttpClient? httpClient = null,
        string? baseDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var (base64, mimeType) = await LoadAsBase64Async(mediaUri, httpClient, baseDirectory, cancellationToken).ConfigureAwait(false);
        return new EmbeddedResource
        {
            Resource = new ResourceContents
            {
                Uri = embeddedResourceUri,
                MimeType = mimeType,
                Blob = base64
            }
        };
    }

    /// <summary>
    /// Parses an RFC 2397 Data URI into raw binary bytes and detected media type.
    /// Format: data:[&lt;mediatype&gt;][;base64],&lt;data&gt;
    /// </summary>
    public static bool TryParseDataUri(string uri, out byte[] bytes, out string mimeType)
    {
        bytes = Array.Empty<byte>();
        mimeType = "text/plain";

        if (string.IsNullOrWhiteSpace(uri) || !uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var commaIndex = uri.IndexOf(',');
        if (commaIndex < 5)
        {
            return false;
        }

        var header = uri.Substring(5, commaIndex - 5);
        var dataPart = uri.Substring(commaIndex + 1);

        var isBase64 = false;
        var parts = header.Split(';');

        var mediaTypePart = parts.Length > 0 ? parts[0].Trim() : "";
        mimeType = string.IsNullOrWhiteSpace(mediaTypePart) ? "text/plain" : mediaTypePart;

        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Trim().Equals("base64", StringComparison.OrdinalIgnoreCase))
            {
                isBase64 = true;
                break;
            }
        }

        try
        {
            if (isBase64)
            {
                // Remove any inline whitespace or newlines
                var cleanBase64 = dataPart.Replace(" ", "").Replace("\r", "").Replace("\n", "");
                bytes = Convert.FromBase64String(cleanBase64);
                return true;
            }
            else
            {
                var unescaped = Uri.UnescapeDataString(dataPart);
                bytes = Encoding.UTF8.GetBytes(unescaped);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Maps a file path or extension to a standard IANA MIME type.
    /// Falls back to "application/octet-stream" if unrecognized.
    /// </summary>
    public static string GetMimeTypeFromExtension(string pathOrExtension)
    {
        if (string.IsNullOrWhiteSpace(pathOrExtension))
        {
            return "application/octet-stream";
        }

        var ext = Path.GetExtension(pathOrExtension);
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = pathOrExtension.StartsWith(".") ? pathOrExtension : "." + pathOrExtension;
        }

        // Clean any query strings if URL was passed (e.g. .jpg?v=1)
        var queryIdx = ext.IndexOfAny(new[] { '?', '#' });
        if (queryIdx >= 0)
        {
            ext = ext.Substring(0, queryIdx);
        }

        if (_extensionToMime.TryGetValue(ext, out var mime))
        {
            return mime;
        }

        return "application/octet-stream";
    }
}
