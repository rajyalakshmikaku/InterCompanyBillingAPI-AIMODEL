using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class OutstandingAmountDto
    {
        public string? ReferenceNumbers { get; set; }
        public string CompanyName { get; set; }
        public decimal OutstandingAmount { get; set; }
        public int NumberOfInvoices { get; set; }
        public decimal OverdueAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Type { get; set; }
        public string ReceivableCompany { get; set; }
    }
}
