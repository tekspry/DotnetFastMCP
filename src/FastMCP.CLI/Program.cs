using System.CommandLine;
using System.Reflection;

var rootCommand = new RootCommand("Command-line interface for the DotnetFastMCP framework.");

// Version Command - mirrors `fastmcp version`
var versionCommand = new Command("version", "Display version information.");
versionCommand.SetHandler(() => 
{
    var version = Assembly.GetExecutingAssembly().GetName().Version;
    Console.WriteLine($"FastMCP.CLI Version: {version}");
    // In a real implementation, we would also load the core library version.
});

rootCommand.AddCommand(versionCommand);

// TODO: Implement the 'run' command to load and start a server from a project.
// var runCommand = new Command("run", "Run an MCP server.");
// rootCommand.AddCommand(runCommand);

return await rootCommand.InvokeAsync(args);
