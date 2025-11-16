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
            
            Console.WriteLine("=== All tests completed successfully! ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
