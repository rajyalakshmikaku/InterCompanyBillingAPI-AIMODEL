using InterCompanyBillingSulution.Models;

namespace InterCompanyBillingSulution_Core.Services
{

    public class PythonAIService
    {
        private readonly HttpClient _httpClient;

        public PythonAIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<DataQuery?> ClassifyAsync(string message)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "http://127.0.0.1:8000/classify",
                new ChatRequest { Message = message });

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<DataQuery>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<string> GenerateResponse(string message, CancellationToken ct = default)
        {
            var request = new ChatRequest
            {
                Message = message
            };

            var response = await _httpClient.PostAsJsonAsync(
                "http://127.0.0.1:8000/generate",
                request, ct);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<ChatResponse>();

            return result?.Response ?? "No response from Python AI";
        }
    }
}
