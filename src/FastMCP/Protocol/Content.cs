using System.Text.Json.Serialization;
namespace FastMCP.Protocol;
// Base class (or union simplified)
// 1. Enable Polymorphism
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextContent), typeDiscriminator: "text")]
[JsonDerivedType(typeof(ImageContent), typeDiscriminator: "image")]
[JsonDerivedType(typeof(EmbeddedResource), typeDiscriminator: "resource")]
public abstract class ContentItem
{
    [JsonIgnore]
    public abstract string Type { get; }
}
public class TextContent : ContentItem
{
    public override string Type => "text";
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}
public class ImageContent : ContentItem
{
    public override string Type => "image";
    
    [JsonPropertyName("data")]
    public string Data { get; set; } = ""; // Base64 string
    
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "";
}
public class EmbeddedResource : ContentItem
{
    public override string Type => "resource";
    
    [JsonPropertyName("resource")]
    public ResourceContents Resource { get; set; } = new();
}