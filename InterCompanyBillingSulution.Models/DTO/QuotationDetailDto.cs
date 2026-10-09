using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class QuotationDetailDto
    {
        public int DetailId { get; set; }
        public string DetailReferenceNumber { get; set; }
        public decimal QuotationAmount { get; set; }
        public decimal QuotationQuantity { get; set; }
        public decimal UnitRate { get; set; }
        public decimal Discount { get; set; }
        public string DiscountType { get; set; }
        public decimal Tax { get; set; }
        public int InventoryId { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
    }
}
