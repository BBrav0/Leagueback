using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using backend.Models; // Or the namespace where your Models.cs file is

namespace backend // This should be your project's namespace
{
    // This attribute is essential. It makes the class visible to JavaScript.
    [ComVisible(true)]
    public class BackendApiBridge
    {
        private readonly RiotApiService _riotApiService;

        public BackendApiBridge()
        {
            // IMPORTANT: For a real application, you'll want a more secure
            // way to store and access your API key than hardcoding it.
            string apiKey = "YOUR_RIOT_API_KEY_HERE"; 
            
            if(string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_RIOT_API_KEY_HERE")
            {
                // This prevents the app from crashing if the key isn't set.
                // The frontend will receive a clear error message.
                _riotApiService = null; 
                return;
            }

            _riotApiService = new RiotApiService(apiKey);
        }

        // This is the public C# method that your Next.js code will be able to call.
        public async Task<string> GetAccount(string gameName, string tagLine)
        {
            if (_riotApiService == null)
            {
                return JsonSerializer.Serialize(new { error = "API Key is not configured in the C# backend." });
            }
            
            try
            {
                // We call the real service to get the data.
                AccountDto account = await _riotApiService.GetAccountByRiotIdAsync(gameName, tagLine);
                
                // We convert the C# object into a JSON string to send to the frontend.
                return JsonSerializer.Serialize(account);
            }
            catch (Exception ex)
            {
                // If any error happens in C#, we catch it and send back a
                // clean error message as a JSON string.
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }
    }
}