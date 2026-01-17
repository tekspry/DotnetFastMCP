using FastMCP.Attributes;

public static class SubServerTools
{
    [McpTool]
    public static string CreateIssue(string title) => $"[GitHub] Issue created: {title}";

    [McpTool]
    public static string GetRepo(string owner, string name) => $"[GitHub] Repo {owner}/{name}";
}
