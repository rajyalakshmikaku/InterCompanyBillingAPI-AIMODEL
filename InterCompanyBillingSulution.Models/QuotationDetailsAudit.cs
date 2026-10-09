using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
    public class QuotationDetailsAudit
    {

        public int Id { get; set; }
        public int QuotationId { get; set; }
        public string CompanyId { get; set; }
        public string ReferenceNumber { get; set; }
        public string InventoryId { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal QuotationQuantity { get; set; }
        public int QuotationAmount { get; set; }
        public decimal UnitRate { get; set; }
        public int VAT { get; set; }
        public decimal? Discount { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }  
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
        public string Action { get; set; }
        public string ActionBy { get; set; }
         public string? IPaddressOfSystem { get; set; }
        public DateTime ActionDate { get; set; }


    }
}
