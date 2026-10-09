using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class QuotationActionRequestDTO
    {
        public string QuotationId { get; set; }
        public string? Type { get; set; }
        public string? Reason { get; set; }
    }

}
