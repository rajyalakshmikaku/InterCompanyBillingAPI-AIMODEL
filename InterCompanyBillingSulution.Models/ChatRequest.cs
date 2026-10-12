using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
    public class ChatRequest
    {
        public string Message { get; set; }

        // Recent turns of the conversation (oldest first), so follow-ups like "give those users names" make sense.
        public List<ChatTurn>? History { get; set; }
    }

    public class ChatTurn
    {
        public string? Role { get; set; }   // "user" | "assistant"
        public string? Text { get; set; }
    }
}
