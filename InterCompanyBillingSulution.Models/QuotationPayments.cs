using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
    public class QuotationPayments
    {
        public int Id { get; set; }
        public string QuotationRefNo { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal TotatlDue { get; set; }
        public decimal PaidAmount { get; set; } 

        public DateTime PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        
    }
}
