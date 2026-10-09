using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class RecordQuotationPaymentRequest
    {
        public int QuotationRefNo { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal PaymentAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }

    }
}
