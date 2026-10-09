using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{


    public class QuotationInvoiceDetails
    {
        public int Id { get; set; }
        public Guid GUID { get; set; } = Guid.NewGuid();
        public string? CompanyId { get; set; }
        [Required]
        public string ReferenceNumber { get; set; }
        public string? Description { get; set; }
        public decimal? QuotationQuantity { get; set; }
        public int QuotationAmount { get; set; }
        public decimal? UnitRate { get; set; }
        public int VAT { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string Category { get; set; }
        public string? InventoryId { get; set; }
        public decimal? Discount { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }

    }


}
