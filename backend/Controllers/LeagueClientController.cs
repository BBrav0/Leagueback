using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using backend; // Access PlayerCache & UserCache

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeagueClientController : ControllerBase
    {
        private readonly ILogger<LeagueClientController> _logger;

        public LeagueClientController(ILogger<LeagueClientController> logger)
        {
            _logger = logger;
        }

        [HttpGet("league-client-info")]
        public async Task<IActionResult> GetLeagueClientInfo()
        {
            Console.WriteLine("League client info endpoint called");
            
            // 1. Check for League of Legends process
            var leagueProcess = Process.GetProcessesByName("LeagueClient").FirstOrDefault()
                                ?? Process.GetProcessesByName("LeagueClientUx").FirstOrDefault();

            if (leagueProcess == null)
            {
                // Try to load from user cache (never expires)
                var userCache = await UserCache.LoadCacheDataAsync();
                if (userCache != null)
                {
                    Console.WriteLine("Returning cached player info (non-expiring)");
                    return Ok(new {
                        gameName = userCache.GameName,
                        tagLine = userCache.TagLine,
                        isAvailable = true,
                        fromCache = true
                    });
                }
                Console.WriteLine("League of Legends process not found and no user cache available");
                _logger.LogWarning("League of Legends process not found and no user cache available.");
                return NotFound(new { message = "League of Legends process not found and no user cache available." });
            }

            Console.WriteLine($"Found League process: {leagueProcess.ProcessName}");

            // 2. Construct and verify the lockfile path
            string lockfilePath = Path.Combine(
                "C:", "Riot Games", "League of Legends", "lockfile"
            );

            Console.WriteLine($"Looking for lockfile at: {lockfilePath}");

            if (!System.IO.File.Exists(lockfilePath))
            {
                Console.WriteLine("Lockfile not found");
                _logger.LogWarning($"Lockfile not found at path: {lockfilePath}");
                return NotFound(new { message = "Lockfile not found. The client might be starting up." });
            }

            try
            {
                // 3. Read lockfile and extract connection info
                string lockfileContent;
                using (var stream = new FileStream(lockfilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    lockfileContent = await reader.ReadToEndAsync();
                }
                
                Console.WriteLine($"Lockfile content: {lockfileContent}");

                string[] parts = lockfileContent.Split(':');
                if (parts.Length < 4)
                {
                    Console.WriteLine($"Invalid lockfile format. Got {parts.Length} parts");
                    _logger.LogError($"Invalid lockfile format. Expected at least 4 parts, got {parts.Length}");
                    return BadRequest(new { message = "Lockfile is in an unexpected format." });
                }

                string port = parts[2];
                string password = parts[3];

                Console.WriteLine($"Extracted port: {port}");

                // 4. Configure HttpClient to trust the client's self-signed SSL certificate
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                using (var client = new HttpClient(handler))
                {
                    var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"riot:{password}"));
                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");

                    // 5. Make the API call
                    string apiUrl = $"https://127.0.0.1:{port}/lol-summoner/v1/current-summoner";
                    Console.WriteLine($"Attempting to call LCU API at: {apiUrl}");

                    var response = await client.GetAsync(apiUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"API call failed with status {response.StatusCode}. Content: {errorContent}");
                        _logger.LogError($"LCU API returned non-success status: {response.StatusCode}. Content: {errorContent}");
                        return StatusCode((int)response.StatusCode, new { message = "Failed to get summoner data from League Client API." });
                    }

                    var summonerData = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Received summoner data: {summonerData}");

                    var summoner = JsonSerializer.Deserialize<JsonElement>(summonerData);
                    var gameName = summoner.GetProperty("gameName").GetString();
                    var tagLine = summoner.GetProperty("tagLine").GetString();

                    // Save to user cache (never expires)
                    await UserCache.SaveCacheDataAsync(
                        summoner.GetProperty("puuid").GetString() ?? string.Empty,
                        gameName ?? string.Empty,
                        tagLine ?? string.Empty
                    );

                    return Ok(new
                    {
                        gameName = gameName,
                        tagLine = tagLine,
                        isAvailable = true,
                        fromCache = false
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                _logger.LogError(ex, "An unexpected error occurred while getting League client info.");
                return StatusCode(500, new { message = "An internal server error occurred.", details = ex.Message });
            }
        }
    }
} 