using System.Text.Json;
using System.Text.RegularExpressions;

namespace InterCompanyBillingSulution_Core.Services
{
    // Talks to the internal AI server (settings: "JanAI" section of appsettings.json).
    // Kept under its old name so the controller and DI registration do not change.
    public class PythonAIService
    {
        private const string AssistantPrompt =
            "You are the assistant of an inter-company billing application. Be accurate and helpful, in plain text. " +
            "Use short numbered steps for how-to questions. Keep answers under 200 words unless the user asks for detail. " +
            "Never invent screens, buttons or numbers that were not given to you.";

        private const string ClassifyPrompt = """
            You convert questions about the data of an inter-company billing application into JSON.
            Reply with ONLY one JSON object with these keys (use null when not mentioned):
            entity: "quotations" | "invoices" | "credit_notes" | "customers" | "companies" | "users" | "products" | "none" (none = not a question about application data)
            metric: "count" (how many) | "total_amount" (sum of money) | "average_amount" | "list" (show/list/latest/highest/lowest records)
            status: Approved, Rejected, Paid, Created, Updated, Overdue, Expired, Active (pending means Created)
            customer: a customer name mentioned, company: a company name mentioned
            period: today | yesterday | this_week | this_month | last_month | this_year | last_year
            sort: latest | oldest | highest | lowest (for list), top: number of records (for list)
            Examples:
            "how many approved quotations" -> {"entity":"quotations","metric":"count","status":"Approved"}
            "total value of paid invoices this month" -> {"entity":"invoices","metric":"total_amount","status":"Paid","period":"this_month"}
            "show the latest 3 quotations for Acme" -> {"entity":"quotations","metric":"list","customer":"Acme","sort":"latest","top":3}
            "which is the highest invoice" -> {"entity":"invoices","metric":"list","sort":"highest","top":1}
            "average quotation amount last year" -> {"entity":"quotations","metric":"average_amount","period":"last_year"}
            "give me the names of all users" -> {"entity":"users","metric":"list","sort":"latest","top":25}
            "list active customers" -> {"entity":"customers","metric":"list","status":"Active","sort":"latest","top":25}
            When the input has "Previous question: ... | Current question: ...", the current question refers back to the previous one; keep the previous entity and filters:
            "Previous question: total no of users | Current question: give those users names" -> {"entity":"users","metric":"list","sort":"latest","top":25}
            "Previous question: how many approved quotations | Current question: show them" -> {"entity":"quotations","metric":"list","status":"Approved","sort":"latest","top":10}
            "how to create a quotation" -> {"entity":"none"}
            "what is python" -> {"entity":"none"}
            """;

        private readonly HttpClient _httpClient;
        private readonly string _chatUrl;
        private readonly string _model;

        public PythonAIService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            // Same "JanAI" settings the server guide describes: the full OpenAI-style chat URL and the model name.
            _chatUrl = config["JanAI:BaseUrl"] ?? "https://192.168.9.179/v1/chat/completions";
            _model = config["JanAI:Model"] ?? "qwen2.5:7b";
        }

        public async Task<DataQuery?> ClassifyAsync(string message, CancellationToken ct = default)
        {
            var text = await ChatAsync(ClassifyPrompt, message, json: true, maxTokens: 150, temperature: 0, ct);
            try
            {
                return JsonSerializer.Deserialize<DataQuery>(text,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public async Task<string> GenerateResponse(string message, CancellationToken ct = default, bool general = false)
        {
            // General questions: no application context, just a direct answer.
            var system = general
                ? "You are a knowledgeable, direct assistant. (Today is " + DateTime.Today.ToString("d MMMM yyyy") + "; use this only if asked about dates.) " +
                  "Answer the user's question directly and concisely, as the first sentence, in plain text. " +
                  "The user may make spelling mistakes or use abbreviations and is based in India: interpret them the most likely way " +
                  "(for example 'andra cm' means the Chief Minister of Andhra Pradesh). " +
                  "If a question is ambiguous, answer its most common meaning instead of asking for clarification. " +
                  "Never describe or classify the question, never say it is unrelated to anything, and never use placeholders such as [name]. " +
                  "Only for questions about who currently holds a position or about very recent events, state the latest answer you know " +
                  "and add one short note that it may have changed. For every other question add no disclaimer at all."
                : AssistantPrompt;
            var text = await ChatAsync(system, message, json: false, maxTokens: 700, temperature: general ? 0.3 : 0.2, ct);
            return string.IsNullOrWhiteSpace(text) ? "No response from the AI model." : text;
        }

        private async Task<string> ChatAsync(string system, string user, bool json, int maxTokens,
            double temperature, CancellationToken ct)
        {
            var body = new Dictionary<string, object>
            {
                ["model"] = _model,
                ["stream"] = false,
                ["temperature"] = temperature,
                ["max_tokens"] = maxTokens,
                ["messages"] = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user", content = user },
                },
            };
            if (json) body["response_format"] = new { type = "json_object" };

            using var response = await _httpClient.PostAsJsonAsync(_chatUrl, body, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var content = doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message").GetProperty("content").GetString() ?? "";
            // Some models (e.g. qwen3) may emit reasoning; never show it to users.
            content = Regex.Replace(content, "<think>.*?</think>", "", RegexOptions.Singleline);
            return content.Split("</think>").Last().Trim();
        }
    }
}
