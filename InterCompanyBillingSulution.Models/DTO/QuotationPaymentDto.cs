using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class QuotationPaymentDto
    {
        public int PaymentId { get; set; }
        public string QuotationRefNo { get; set; }
        public string TransactionId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal PaidAmount { get; set; }
    
        public decimal TotalAmount { get; set; }
        public decimal TotatlDue { get; set; }
    }
}
