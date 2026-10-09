using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class CreditNoteDetailsDTO
    {
        public string ItemCode { get; set; }
        public string Category { get; set; }
        public string ItemName { get; set; }

        public decimal? ItemQuantity { get; set; }
        public decimal? UnitRate { get; set; }
        public decimal? Discount { get; set; }
        public int? VAT { get; set; }

        public decimal? ItemAmount { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
    }
}
