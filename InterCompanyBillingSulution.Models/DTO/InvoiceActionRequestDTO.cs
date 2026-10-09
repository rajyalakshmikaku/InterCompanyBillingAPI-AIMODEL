using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class InvoiceActionRequestDTO
    {
        public int InvoiceId { get; set; }
        public string? Type { get; set; }
        public string? Reason { get; set; }
        public string? cancelledBy { get; set; }

        public string? TemplateUrl { get; set; }
    }
}
