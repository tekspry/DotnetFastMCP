using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace McpIntegrationTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== DotnetFastMCP BasicServer Integration Test ===\n");
        
        using var client = new HttpClient();
        
        try
        {
            // Test 1: Root endpoint
            Console.WriteLine("Test 1: GET /");
            Console.WriteLine("Sending request...");
            var response1 = await client.GetAsync("http://localhost:5000/");
            var content1 = await response1.Content.ReadAsStringAsync();
            Console.WriteLine($"Status: {response1.StatusCode}");
            Console.WriteLine($"Response:\n{content1}\n");
            
            // Test 2: Add tool with array parameters
            Console.WriteLine("Test 2: POST /mcp - Add(5, 3) with array params");
            var request2 = new
            {
                jsonrpc = "2.0",
                method = "Add",
                @params = new int[] { 5, 3 },
                id = 1
            };
            var json2 = JsonSerializer.Serialize(request2);
            Console.WriteLine($"Request: {json2}");
            var content2 = new StringContent(json2, Encoding.UTF8, "application/json");
            var response2 = await client.PostAsync("http://localhost:5000/mcp", content2);
            var body2 = await response2.Content.ReadAsStringAsync();
            Console.WriteLine($"Status: {response2.StatusCode}");
            Console.WriteLine($"Response: {body2}\n");
            
            // Test 3: Add tool with named parameters
            Console.WriteLine("Test 3: POST /mcp - Add with named params");
            var request3 = new
            {
                jsonrpc = "2.0",
                method = "Add",
                @params = new { a = 10, b = 20 },
                id = 2
            };
            var json3 = JsonSerializer.Serialize(request3);
            Console.WriteLine($"Request: {json3}");
            var content3 = new StringContent(json3, Encoding.UTF8, "application/json");
            var response3 = await client.PostAsync("http://localhost:5000/mcp", content3);
            var body3 = await response3.Content.ReadAsStringAsync();
            Console.WriteLine($"Status: {response3.StatusCode}");
            Console.WriteLine($"Response: {body3}\n");
            
            // Test 4: Error - method not found
            Console.WriteLine("Test 4: POST /mcp - NonExistentMethod (should error)");
            var request4 = new
            {
                jsonrpc = "2.0",
                method = "NonExistentMethod",
                @params = new object[] { },
                id = 3
            };
            var json4 = JsonSerializer.Serialize(request4);
            Console.WriteLine($"Request: {json4}");
            var content4 = new StringContent(json4, Encoding.UTF8, "application/json");
            var response4 = await client.PostAsync("http://localhost:5000/mcp", content4);
            var body4 = await response4.Content.ReadAsStringAsync();
            Console.WriteLine($"Status: {response4.StatusCode}");
            Console.WriteLine($"Response: {body4}\n");

            // Test 5: Get Resource (config)
            Console.WriteLine("Test 5: POST /mcp - GetResource 'config'");
            var request5 = new
            {
                jsonrpc = "2.0",
                method = "config", 
                @params = new { },
                id = 4
            };
            var json5 = JsonSerializer.Serialize(request5);
            Console.WriteLine($"Request: {json5}");
            var content5 = new StringContent(json5, Encoding.UTF8, "application/json");
            var response5 = await client.PostAsync("http://localhost:5000/mcp", content5);
            var body5 = await response5.Content.ReadAsStringAsync();
            Console.WriteLine($"Status: {response5.StatusCode}");
            Console.WriteLine($"Response: {body5}\n");

            // Test 6: Invalid JSON (Parse Error)
            Console.WriteLine("Test 6: POST /mcp - Invalid JSON");
            var invalidJson = "{ \"jsonrpc\": \"2.0\", \"method\": \"Add\", \"params\": [1, 2] "; // Missing closing brace
            Console.WriteLine($"Request: {invalidJson}");
            var content6 = new StringContent(invalidJson, Encoding.UTF8, "application/json");
            var response6 = await client.PostAsync("http://localhost:5000/mcp", content6);
            var body6 = await response6.Content.ReadAsStringAsync();
            Console.WriteLine($"Status: {response6.StatusCode}");
            Console.WriteLine($"Response: {body6}\n");

            // Test 7: List Prompts
            Console.WriteLine("Test 7: POST /mcp - prompts/list");
            var request7 = new
            {
                jsonrpc = "2.0",
                method = "prompts/list",
                @params = new { },
                id = 5
            };
            var json7 = JsonSerializer.Serialize(request7);
            Console.WriteLine($"Request: {json7}");
            var content7 = new StringContent(json7, Encoding.UTF8, "application/json");
            var response7 = await client.PostAsync("http://localhost:5000/mcp", content7);
            var body7 = await response7.Content.ReadAsStringAsync();
            Console.WriteLine($"Status: {response7.StatusCode}");
            Console.WriteLine($"Response: {body7}\n");

            // Test 8: Get Prompt (analyze_code)
            Console.WriteLine("Test 8: POST /mcp - prompts/get");
            var request8 = new
            {
                jsonrpc = "2.0",
                method = "prompts/get",
                @params = new 
                { 
                    name = "analyze_code",
                    arguments = new { code = "Console.WriteLine(\"Hello\");" } 
                },
                id = 6
            };
            var json8 = JsonSerializer.Serialize(request8);
            Console.WriteLine($"Request: {json8}");
            var content8 = new StringContent(json8, Encoding.UTF8, "application/json");
            var response8 = await client.PostAsync("http://localhost:5000/mcp", content8);
            var body8 = await response8.Content.ReadAsStringAsync();
            Console.WriteLine($"Status: {response8.StatusCode}");
            Console.WriteLine($"Response: {body8}\n");

            
            Console.WriteLine("=== All tests completed successfully! ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
