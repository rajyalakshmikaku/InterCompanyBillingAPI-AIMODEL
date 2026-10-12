using InterCompanyBillingSulution.DataAcess.Data;
using Microsoft.EntityFrameworkCore;

namespace InterCompanyBillingSulution_Core.Services
{
    public class DataQuery
    {
        public string? Entity { get; set; }
        public string? Metric { get; set; }
        public string? Status { get; set; }
    }

    // Runs only whitelisted, parameterised queries. The LLM never writes SQL.
    public class DataQueryService
    {
        private readonly InterCompanyBillingDbContext _context;

        public DataQueryService(InterCompanyBillingDbContext context)
        {
            _context = context;
        }

        private static readonly string[] Statuses =
            { "Approved", "Rejected", "Paid", "Created", "Updated", "Overdue", "Expired", "Active" };

        // Fast path: obvious "how many / total <thing>" questions skip the LLM entirely.
        public static DataQuery? TryParse(string message)
        {
            var m = message.ToLowerInvariant();
            string? entity =
                m.Contains("quotation") ? "quotations" :
                m.Contains("invoice") ? "invoices" :
                m.Contains("credit note") ? "credit_notes" :
                m.Contains("customer") ? "customers" :
                m.Contains("compan") ? "companies" :
                m.Contains("user") ? "users" : null;
            if (entity == null) return null;

            bool asksData = m.Contains("how many") || m.Contains("total") || m.Contains("count") || m.Contains("number of");
            if (!asksData) return null;

            bool amount = m.Contains("amount") || m.Contains("value") || m.Contains("worth") || m.Contains("sum");
            var status = Statuses.FirstOrDefault(s => m.Contains(s.ToLowerInvariant()));
            return new DataQuery { Entity = entity, Metric = amount ? "total_amount" : "count", Status = status };
        }

        // A data answer is only allowed when the user explicitly asks for a count/total.
        public static bool AsksForData(string message)
        {
            var m = message.ToLowerInvariant();
            return m.Contains("how many") || m.Contains("total") || m.Contains("count") || m.Contains("number of");
        }

        // Answers a how-to question straight from the app's own help text (no LLM, so it is instant).
        // Returns null when no help screen matches the question.
        public async Task<string?> GetHelpAnswerAsync(string message)
        {
            List<(string? ScreenName, string? Description)> relevant;
            try
            {
                relevant = await FindRelevantHelpAsync(message, 1);
            }
            catch (Exception)
            {
                return null; // help lookup is best-effort: on a DB hiccup, let the AI answer instead
            }
            if (relevant.Count == 0) return null;

            var text = System.Text.RegularExpressions.Regex.Replace(relevant[0].Description ?? "", "<[^>]+>", "\n");
            text = System.Net.WebUtility.HtmlDecode(text);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n+", "\n\n").Trim();
            return string.IsNullOrEmpty(text) ? null : $"{relevant[0].ScreenName}\n\n{text}";
        }

        private async Task<List<(string? ScreenName, string? Description)>> FindRelevantHelpAsync(string message, int take)
        {
            var words = System.Text.RegularExpressions.Regex.Matches(message.ToLowerInvariant(), "[a-z]{4,}")
                .Select(x => x.Value)
                .Where(w => w != "how" && w != "what" && w != "does" && w != "with" && w != "this"
                         && w != "generate" && w != "create" && w != "make" && w != "help")
                .Distinct()
                .ToList();

            // Greetings and other messages with no meaningful keyword never need the database.
            if (words.Count == 0) return new();

            var rows = await _context.HelpControlTable
                .Where(h => h.Status == "Y")
                .Select(h => new { h.ScreenName, h.Description })
                .ToListAsync();

            return rows
                .Select(h => new
                {
                    h.ScreenName,
                    h.Description,
                    // Only a screen-name hit counts: a word merely appearing somewhere in a long help
                    // description (e.g. "number") must not pull in an unrelated screen.
                    Score = words.Count(w => (h.ScreenName ?? "").ToLowerInvariant().Contains(w)),
                    Mentions = words.Count(w => (h.Description ?? "").ToLowerInvariant().Contains(w))
                })
                .Where(h => h.Score > 0)
                .OrderByDescending(h => h.Score)
                .ThenByDescending(h => h.Mentions)
                .Take(take)
                .Select(h => (h.ScreenName, h.Description))
                .ToList();
        }

        // Builds a prompt that grounds a how-to answer in the app's own help text.
        public async Task<string> BuildHelpPromptAsync(string message)
        {
            var words = System.Text.RegularExpressions.Regex.Matches(message.ToLowerInvariant(), "[a-z]{4,}")
                .Select(x => x.Value)
                .Where(w => w != "how" && w != "what" && w != "does" && w != "with" && w != "this")
                .Distinct()
                .ToList();

            var rows = await _context.HelpControlTable
                .Where(h => h.Status == "Y")
                .Select(h => new { h.ScreenName, h.Description })
                .ToListAsync();

            var relevant = rows
                .Select(h => new
                {
                    h.ScreenName,
                    h.Description,
                    Score = words.Count(w =>
                        (h.ScreenName ?? "").ToLowerInvariant().Contains(w) ||
                        (h.Description ?? "").ToLowerInvariant().Contains(w))
                })
                .Where(h => h.Score > 0)
                .OrderByDescending(h => h.Score)
                .Take(3)
                .ToList();

            if (relevant.Count == 0) return message;

            var help = string.Join("\n\n", relevant.Select(h => $"[{h.ScreenName}]\n{h.Description}"));
            if (help.Length > 5000) help = help[..5000];

            return "You are the help assistant of an inter-company billing application. " +
                   "Answer the question using the help text below. If it does not cover the question, say so briefly.\n\n" +
                   $"HELP TEXT:\n{help}\n\nQUESTION: {message}";
        }

        public async Task<string?> AnswerAsync(DataQuery q)
        {
            var status = string.IsNullOrWhiteSpace(q.Status) ? null : q.Status.Trim();
            var suffix = status == null ? "" : $" with status '{status}'";
            var wantsAmount = string.Equals(q.Metric, "total_amount", StringComparison.OrdinalIgnoreCase);

            switch (q.Entity?.ToLowerInvariant())
            {
                case "quotations":
                {
                    var query = _context.BillingQuotations.Where(x => x.DeletedDate == null);
                    if (status != null) query = query.Where(x => x.Status == status);
                    return wantsAmount
                        ? $"The total quotation amount{suffix} is {await query.SumAsync(x => (decimal?)x.TotalAmount) ?? 0:N2}."
                        : $"There are {await query.CountAsync()} quotations{suffix}.";
                }
                case "invoices":
                {
                    var query = _context.QuotationInvoice.AsQueryable();
                    if (status != null) query = query.Where(x => x.Status == status);
                    return wantsAmount
                        ? $"The total invoice amount{suffix} is {await query.SumAsync(x => (decimal?)x.TotalAmount) ?? 0:N2}."
                        : $"There are {await query.CountAsync()} invoices{suffix}.";
                }
                case "credit_notes":
                {
                    var query = _context.CreditNoteHeader.AsQueryable();
                    return wantsAmount
                        ? $"The total credit note amount is {await query.SumAsync(x => (decimal?)x.TotalAmount) ?? 0:N2}."
                        : $"There are {await query.CountAsync()} credit notes.";
                }
                case "customers":
                {
                    var query = _context.Customers.AsQueryable();
                    if (status != null) query = query.Where(x => x.Status == status);
                    return $"There are {await query.CountAsync()} customers{suffix}.";
                }
                case "companies":
                {
                    var query = _context.Companies.AsQueryable();
                    if (status != null) query = query.Where(x => x.Status == status);
                    return $"There are {await query.CountAsync()} companies{suffix}.";
                }
                case "users":
                {
                    var query = _context.Users.AsQueryable();
                    if (status != null) query = query.Where(x => x.Status == status);
                    return $"There are {await query.CountAsync()} users{suffix}.";
                }
                default:
                    return null;
            }
        }
    }
}
