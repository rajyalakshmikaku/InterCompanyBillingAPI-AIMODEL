using System.Globalization;
using System.Text.RegularExpressions;
using InterCompanyBillingSulution.DataAcess.Data;
using Microsoft.EntityFrameworkCore;

namespace InterCompanyBillingSulution_Core.Services
{
    // What the model extracts from a data question. The model never writes SQL: it only fills these fields,
    // and DataQueryService turns them into whitelisted, parameterised EF queries.
    public class DataQuery
    {
        public string? Entity { get; set; }    // quotations | invoices | credit_notes | customers | companies | users | products
        public string? Metric { get; set; }    // count | total_amount | average_amount | list
        public string? Status { get; set; }
        public string? Customer { get; set; }  // customer name contains
        public string? Company { get; set; }   // company name contains
        public string? Period { get; set; }    // today | this_week | this_month | last_month | this_year | last_year
        public string? Sort { get; set; }      // latest | oldest | highest | lowest (for list)
        public int? Top { get; set; }          // rows for list
    }

    public class DataQueryService
    {
        private readonly InterCompanyBillingDbContext _context;

        public DataQueryService(InterCompanyBillingDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------------- knowledge (how-to) questions

        // Short map of the whole application; always given to the model so any app question has context.
        private const string AppOverview =
            "APPLICATION OVERVIEW (Inter-Company Billing):\n" +
            "Workflow: a user creates a Quotation (Quotation screen) -> it is reviewed on Quotation Approval (Approved or Rejected) -> " +
            "an approved quotation becomes an Invoice (Proforma invoices also exist) -> invoices are reviewed on Invoice Approval -> " +
            "payments are recorded and the invoice becomes Paid -> Credit Notes can be raised against invoices.\n" +
            "Statuses seen in the app: Created, Updated, Approved, Rejected, Paid, Overdue, Expired, Active.\n" +
            "Menu (group: screens): General: Dashboard, Reports Dashboard, Complete Order List, Chat. " +
            "Billing: Quotation, Quotation Approval, Invoice Approval, Billing Overview, Deleted (deleted quotations), Bulk Upload (historical backlog upload). " +
            "Master Data: Customers, Companies (receiving entities), Products (inventory items), Master (lookup data). " +
            "Administration: Roles, User Verification (approve newly registered users), User Management (map screens to roles and companies), Audit Log (history of changes).\n" +
            "Access: users register, an admin verifies them, and each role only sees the screens mapped to it. Quotations belong to a company and a customer, " +
            "contain product line items with VAT and optional discount, and can be viewed, edited, downloaded as PDF or deleted.\n" +
            "Detailed steps and field descriptions come from the HELP TEXT below when present; do not invent buttons or fields that are not mentioned.";

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "what", "how", "does", "with", "this", "that", "from", "have", "should", "would", "could", "about", "which",
            "when", "where", "there", "their", "your", "into", "than", "then", "them", "they", "will", "can", "the",
            "and", "for", "are", "was", "were", "you", "please", "tell", "explain", "show", "give", "make", "need",
            "want", "using", "use", "used", "brief", "detail", "details", "steps", "step", "process", "work", "works"
        };

        private static List<string> Keywords(string message) =>
            Regex.Matches(message.ToLowerInvariant(), "[a-z]{3,}")
                .Select(m => Stem(m.Value))
                .Where(w => !StopWords.Contains(w))
                .Distinct()
                .ToList();

        // Light stemming so "approvals"/"approving"/"approved" all match "approval" help text.
        private static string Stem(string w)
        {
            foreach (var suffix in new[] { "ing", "ed", "es", "s", "al" })
                if (w.Length > suffix.Length + 3 && w.EndsWith(suffix)) return w[..^suffix.Length];
            return w;
        }

        private async Task<List<(string? ScreenName, string? Description, int Score)>> FindRelevantHelpAsync(string message, int take)
        {
            var words = Keywords(message);
            if (words.Count == 0) return new();

            var rows = await _context.HelpControlTable
                .Where(h => h.Status == "Y")
                .Select(h => new { h.ScreenName, h.Description })
                .ToListAsync();

            return rows
                .Select(h =>
                {
                    var name = (h.ScreenName ?? "").ToLowerInvariant();
                    var desc = (h.Description ?? "").ToLowerInvariant();
                    var nameHits = words.Count(w => name.Contains(w));
                    var descHits = words.Count(w => desc.Contains(w));
                    return (h.ScreenName, h.Description, Score: nameHits * 5 + descHits);
                })
                .Where(h => h.Score >= 3)   // a screen-name match (5 per keyword), or several keywords inside one description
                .OrderByDescending(h => h.Score)
                .Take(take)
                .ToList();
        }

        private static string CleanHelp(string? html)
        {
            var text = Regex.Replace(html ?? "", "<[^>]+>", "\n");
            text = System.Net.WebUtility.HtmlDecode(text);
            return Regex.Replace(text, @"\n\s*\n+", "\n\n").Trim();
        }

        // Prompt that grounds the answer in the app overview and the best-matching help screens.
        // Never throws: if the database is unreachable the overview alone is still used.
        private static readonly Regex AppTerms = new(
            @"quotation|invoice|credit ?note|customer|compan(y|ies)|receiving entit|product|inventory|approv|reject|dashboard|report|" +
            @"\bscreen|\bmenu|\broles?\b|login|log in|sign in|sign out|password|regist|activat|audit|bulk upload|billing|\bvat\b|discount|" +
            @"payment|proforma|template|master data|\busers?\b|this (app|application|system|portal)|inter-?company",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Decided in code (not by the model) so general questions get a plain prompt with no application context.
        public async Task<(string Prompt, string? HelpOnly, bool IsApp)> BuildKnowledgePromptAsync(string message)
        {
            List<(string? ScreenName, string? Description, int Score)> relevant;
            try { relevant = await FindRelevantHelpAsync(message, 3); }
            catch (Exception) { relevant = new(); }

            if (relevant.Count == 0 && !AppTerms.IsMatch(message))
                return (message, null, false);

            var help = string.Join("\n\n", relevant.Select(h => $"[{h.ScreenName}]\n{CleanHelp(h.Description)}"));
            if (help.Length > 5000) help = help[..5000];

            // Without help text the model only knows the overview, so it must not invent click-by-click steps.
            var grounding = help.Length > 0
                ? "Use the HELP TEXT for steps and field details, and ignore any help text that is not relevant to the question. "
                : "No detailed help text is available for this question, so describe only which screen to open and what it is for, " +
                  "in general terms. Do NOT invent button names, tabs, fields or click-by-click steps; say the detailed steps are not documented. ";

            var prompt =
                AppOverview + "\n\n" +
                (help.Length > 0 ? $"HELP TEXT:\n{help}\n\n" : "") +
                "Answer the QUESTION below for a user of this application. " + grounding +
                "Give numbered steps only when they come from the help text. Answer directly; do not describe or classify the question.\n\n" +
                $"QUESTION: {message}";

            return (prompt, help.Length > 0 ? help : null, true);
        }

        // ---------------------------------------------------------------- data questions

        private static readonly string[] Statuses =
            { "Approved", "Rejected", "Paid", "Created", "Updated", "Overdue", "Expired", "Active" };

        private static readonly string[] EntityWords =
            { "quotation", "invoice", "credit note", "customer", "compan", "user", "product", "item", "inventory" };

        private static readonly string[] DataWords =
        {
            "how many", "total", "count", "number of", "sum", "average", "avg", "highest", "lowest", "largest", "smallest",
            "biggest", "latest", "recent", "newest", "oldest", "last ", "list", "top ", "worth", "amount", "value",
            "pending", "overdue", "this month", "this year", "this week", "today", "yesterday", "which",
            "name", "who ", "display", "give", "all ", "details"
        };

        // A short follow-up that points back at earlier results ("give those users names", "and the approved ones?")
        // is joined with the previous user question so it can be classified as a data question.
        public static string WithContext(string message, List<InterCompanyBillingSulution.Models.ChatTurn>? history)
        {
            var m = " " + message.ToLowerInvariant() + " ";
            bool refersBack = new[] { " those ", " these ", " them ", " they ", " that ", " it ", " same ", " ones ", " above ", " previous " }
                .Any(m.Contains);
            if (!refersBack || history == null) return message;

            var previous = history.LastOrDefault(t => t.Role == "user" && !string.IsNullOrWhiteSpace(t.Text))?.Text;
            return string.IsNullOrWhiteSpace(previous) ? message : $"Previous question: {previous} | Current question: {message}";
        }

        // Cheap gate so how-to questions never spend a model call on classification.
        public static bool AsksForData(string message)
        {
            var m = message.ToLowerInvariant();
            if (m.Contains("how to") || m.Contains("how do i") || m.Contains("how can i")) return false;
            return EntityWords.Any(m.Contains) && DataWords.Any(m.Contains);
        }

        // Fast path: short, obvious "how many / total <thing> [status]" questions skip the model entirely.
        public static DataQuery? TryParse(string message)
        {
            var m = message.ToLowerInvariant();
            if (m.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 8) return null;
            if (Regex.IsMatch(m, "today|week|month|year|yesterday| for | of [a-z]+ [a-z]+| by | from ")) return null;

            string? entity =
                m.Contains("quotation") ? "quotations" :
                m.Contains("invoice") ? "invoices" :
                m.Contains("credit note") ? "credit_notes" :
                m.Contains("customer") ? "customers" :
                m.Contains("compan") ? "companies" :
                m.Contains("user") ? "users" : null;
            if (entity == null) return null;

            if (!(m.Contains("how many") || m.Contains("total") || m.Contains("count") || m.Contains("number of"))) return null;

            bool amount = m.Contains("amount") || m.Contains("value") || m.Contains("worth") || m.Contains("sum");
            var status = Statuses.FirstOrDefault(s => m.Contains(s.ToLowerInvariant()));
            return new DataQuery { Entity = entity, Metric = amount ? "total_amount" : "count", Status = status };
        }

        // One shape for every entity so filtering/aggregation is written once.
        private class Row
        {
            public string? Ref { get; set; }
            public string? Customer { get; set; }
            public string? Company { get; set; }
            public string? Status { get; set; }
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }
        }

        private static (DateTime From, DateTime To)? PeriodRange(string? period)
        {
            var today = DateTime.Today;
            switch (period?.Trim().ToLowerInvariant())
            {
                case "today": return (today, today.AddDays(1));
                case "yesterday": return (today.AddDays(-1), today);
                case "this_week":
                    var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                    return (monday, monday.AddDays(7));
                case "this_month": return (new DateTime(today.Year, today.Month, 1), new DateTime(today.Year, today.Month, 1).AddMonths(1));
                case "last_month": return (new DateTime(today.Year, today.Month, 1).AddMonths(-1), new DateTime(today.Year, today.Month, 1));
                case "this_year": return (new DateTime(today.Year, 1, 1), new DateTime(today.Year + 1, 1, 1));
                case "last_year": return (new DateTime(today.Year - 1, 1, 1), new DateTime(today.Year, 1, 1));
                default: return null;
            }
        }

        private static string Money(decimal v) => v.ToString("N2", CultureInfo.InvariantCulture);

        public async Task<string?> AnswerAsync(DataQuery q)
        {
            var entity = q.Entity?.Trim().ToLowerInvariant();
            if (entity == null || entity == "none") return null;

            IQueryable<Row>? rows;
            string label;
            bool hasAmount = true;
            switch (entity)
            {
                case "quotations":
                    label = "quotations";
                    rows = _context.BillingQuotations.Where(x => x.DeletedDate == null).Select(x => new Row
                    { Ref = x.ReferenceNumber, Customer = x.CustomerName, Company = x.CompanyName, Status = x.Status, Amount = x.TotalAmount, Date = x.QuotationDate });
                    break;
                case "invoices":
                    label = "invoices";
                    rows = _context.QuotationInvoice.Select(x => new Row
                    { Ref = x.InvoiceReferenceNumber, Customer = x.CustomerName, Company = x.CompanyName, Status = x.Status, Amount = x.TotalAmount, Date = x.InvoiceDate });
                    break;
                case "credit_notes":
                    label = "credit notes";
                    rows = _context.CreditNoteHeader.Select(x => new Row
                    { Ref = x.CreditNoteReferenceNumber, Customer = x.CustomerName, Company = x.CompanyName, Status = null, Amount = x.TotalAmount, Date = x.CreditNoteDate });
                    break;
                case "customers":
                    label = "customers"; hasAmount = false;
                    rows = _context.Customers.Select(x => new Row
                    { Ref = x.CustomerRefNo, Customer = x.Name + " " + x.SurName, Company = null, Status = x.Status, Date = x.CreatedDate });
                    break;
                case "companies":
                    label = "companies"; hasAmount = false;
                    rows = _context.Companies.Select(x => new Row
                    { Ref = x.CompanyName, Customer = null, Company = x.CompanyName, Status = x.Status, Date = x.CreatedDate });
                    break;
                case "users":
                    label = "users"; hasAmount = false;
                    rows = _context.Users.Select(x => new Row
                    { Ref = x.UserName, Customer = x.Name + " " + x.SurName, Company = null, Status = x.Status, Date = x.CreatedDate });
                    break;
                case "products":
                    label = "products";
                    rows = _context.InventoryItem.Select(x => new Row
                    { Ref = x.ItemCode, Customer = x.ItemName, Company = null, Status = x.Status, Amount = x.Price, Date = x.CreatedDate });
                    break;
                default:
                    return null;
            }

            // Filters, described back to the user so the answer states exactly what was counted.
            var filters = new List<string>();
            var status = string.IsNullOrWhiteSpace(q.Status) ? null : q.Status.Trim();
            if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase)) status = "Created"; // "pending" = awaiting approval
            if (status != null && entity != "credit_notes") { rows = rows.Where(r => r.Status == status); filters.Add($"with status '{status}'"); }
            var customer = string.IsNullOrWhiteSpace(q.Customer) ? null : q.Customer.Trim();
            if (customer != null && entity != "companies") { rows = rows.Where(r => r.Customer != null && r.Customer.Contains(customer)); filters.Add($"for '{customer}'"); }
            var company = string.IsNullOrWhiteSpace(q.Company) ? null : q.Company.Trim();
            if (company != null && (entity is "quotations" or "invoices" or "credit_notes" or "companies"))
            { rows = rows.Where(r => r.Company != null && r.Company.Contains(company)); filters.Add($"of company '{company}'"); }
            var range = PeriodRange(q.Period);
            if (range != null)
            {
                var (from, to) = range.Value;
                rows = rows.Where(r => r.Date >= from && r.Date < to);
                filters.Add($"in {q.Period!.Replace('_', ' ')}");
            }
            var suffix = filters.Count == 0 ? "" : " " + string.Join(" ", filters);

            var metric = q.Metric?.Trim().ToLowerInvariant() ?? "count";
            if (!hasAmount && metric is "total_amount" or "average_amount")
                return $"{char.ToUpper(label[0])}{label[1..]} don't have an amount. There are {await rows.CountAsync()} {label}{suffix}.";

            switch (metric)
            {
                case "total_amount":
                    return $"The total amount of {label}{suffix} is {Money(await rows.SumAsync(r => (decimal?)r.Amount) ?? 0)} " +
                           $"(across {await rows.CountAsync()}; amounts of all currencies are added together).";
                case "average_amount":
                    return $"The average amount of {label}{suffix} is {Money(await rows.AverageAsync(r => (decimal?)r.Amount) ?? 0)}.";
                case "list":
                    var top = Math.Clamp(q.Top ?? 5, 1, 25);
                    var sort = q.Sort?.Trim().ToLowerInvariant();
                    var ordered = sort switch
                    {
                        "highest" when hasAmount => rows.OrderByDescending(r => r.Amount),
                        "lowest" when hasAmount => rows.OrderBy(r => r.Amount),
                        "oldest" => rows.OrderBy(r => r.Date),
                        _ => rows.OrderByDescending(r => r.Date),
                    };
                    var list = await ordered.Take(top).ToListAsync();
                    if (list.Count == 0) return $"No {label} found{suffix}.";
                    var lines = list.Select((r, i) =>
                        $"{i + 1}. {r.Ref}" +
                        (string.IsNullOrWhiteSpace(r.Customer) || entity == "companies" ? "" : $" - {r.Customer}") +
                        (hasAmount ? $" - {Money(r.Amount)}" : "") +
                        (string.IsNullOrWhiteSpace(r.Status) ? "" : $" - {r.Status}") +
                        $" - {r.Date:dd MMM yyyy}");
                    var heading = sort is "highest" or "lowest" or "oldest" ? char.ToUpper(sort[0]) + sort[1..] : "Latest";
                    return $"{heading} {list.Count} {label}{suffix}:\n" + string.Join("\n", lines);
                default:
                    return $"There are {await rows.CountAsync()} {label}{suffix}.";
            }
        }
    }
}
