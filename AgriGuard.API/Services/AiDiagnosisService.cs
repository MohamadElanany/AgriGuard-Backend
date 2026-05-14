using System.Net.Http.Headers;
using System.Text.Json;
using AgriGuard.API.Models;

namespace AgriGuard.API.Services
{
    public class AiDiagnosisService
    {
        private readonly HttpClient _httpClient;

        public AiDiagnosisService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AiPredictionResponse?> PredictDiseaseAsync(string plantName, IFormFile image)
        {
            // Prepare multipart request containing the plant name and uploaded leaf image
            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(plantName), "plant_name");

            using var stream = image.OpenReadStream();
            using var streamContent = new StreamContent(stream);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);

            content.Add(streamContent, "file", image.FileName);

            // Send the request to the FastAPI AI microservice for prediction
            var response = await _httpClient.PostAsync("predict/", content);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine(jsonResponse);

            var result = JsonSerializer.Deserialize<AiPredictionResponse>(
                jsonResponse,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result;
        }
    }
}