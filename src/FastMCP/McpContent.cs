using FastMCP.Protocol;
namespace FastMCP;
public static class McpContent
{
    public static TextContent Text(string text) 
        => new TextContent { Text = text };
    public static ImageContent Image(string base64Data, string mimeType)
        => new ImageContent { Data = base64Data, MimeType = mimeType };
    public static async Task<ImageContent> ImageFromFileAsync(string path, string mimeType)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        var base64 = Convert.ToBase64String(bytes);
        return new ImageContent { Data = base64, MimeType = mimeType };
    }
}