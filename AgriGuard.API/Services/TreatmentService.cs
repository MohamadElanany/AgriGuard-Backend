using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgriGuard.API.DTOs;

namespace AgriGuard.API.Services
{
    public class TreatmentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public TreatmentService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<TreatmentResultDto?> GetTreatmentPlanAsync(GetTreatmentRequestDto dto)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("Gemini API key is missing.");
            }

            var prompt = $@"
You are an agricultural assistant.
Return a treatment plan for a plant disease in valid JSON only.

Disease Name: {dto.DiseaseName}
Crop Name: {dto.CropName}
Country: {dto.Country}
Governorate: {dto.Governorate}

Return JSON with exactly these fields:
- treatmentPlan: string
- preventionTips: array of strings
- recommendedProducts: array of strings
- notes: string

Rules:
- Give practical treatment advice.
- Include prevention tips.
- Include common product types only.
- Keep the answer concise and clear.
- Consider the location when giving advice.
- Return valid JSON only, with no markdown.
";

            var requestBody = new
            {
                contents = new object[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var response = await _httpClient.PostAsync(url, content);

            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Gemini Raw Response:");
            Console.WriteLine(responseJson);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini request failed: {responseJson}");
            }

            using var doc = JsonDocument.Parse(responseJson);

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new Exception("Gemini returned empty content.");
            }

            var result = JsonSerializer.Deserialize<TreatmentResultDto>(
                text,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result;
        }
    }
}