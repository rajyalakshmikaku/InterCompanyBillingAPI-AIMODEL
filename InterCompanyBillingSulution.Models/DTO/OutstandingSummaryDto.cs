using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class OutstandingSummaryDto
    {
        public decimal TotalOwedToCompany { get; set; }
        public int NumberOfInvoices { get; set; }
        public decimal TotalOverdueAmount { get; set; }
    }
}
