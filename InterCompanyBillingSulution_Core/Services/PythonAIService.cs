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

        public async Task<string> GenerateResponse(string message)
        {
            var request = new ChatRequest
            {
                Message = message
            };

            var response = await _httpClient.PostAsJsonAsync(
                "http://127.0.0.1:8000/generate",
                request);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<ChatResponse>();

            return result?.Response ?? "No response from Python AI";
        }
    }
}
