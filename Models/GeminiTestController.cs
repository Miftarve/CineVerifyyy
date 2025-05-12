using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CineVerify.Modules
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeminiTestController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public GeminiTestController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _apiKey = configuration["GeminiApi:ApiKey"];
        }

        [HttpGet("test")]
        public async Task<IActionResult> TestGemini()
        {
            try
            {
                // Simple payload for testing
                var payload = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = "Riassumi brevemente il film Inception in 2 frasi." } } }
                    }
                };

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                // Endpoint concreto senza variabili per evitare problemi
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={_apiKey}";

                // Log the URL without the API key for security
                string logUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=REDACTED";
                Console.WriteLine($"Sending request to {logUrl}");

                var response = await _httpClient.PostAsync(url, requestContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Return raw response to see what's happening
                return Ok(new
                {
                    success = response.IsSuccessStatusCode,
                    statusCode = response.StatusCode.ToString(),
                    response = responseContent
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}