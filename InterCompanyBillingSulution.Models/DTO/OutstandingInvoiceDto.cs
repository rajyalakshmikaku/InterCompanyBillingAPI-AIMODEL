using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class OutstandingInvoiceDto
    {
        public string CompanyOwedTo { get; set; }
        public string OwedByCompany { get; set; }
        public string NumberOfInvoices { get; set; }
        public decimal TotalReceivableAmount { get; set; }
        public decimal OverdueAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public int Year { get; set; }
        public int MonthNo { get; set; }
        public string Month { get; set; }
        public string Type { get; set; }

    }
}
