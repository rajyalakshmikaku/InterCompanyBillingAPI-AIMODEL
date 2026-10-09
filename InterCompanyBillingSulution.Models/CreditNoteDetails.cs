using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
    public class CreditNoteDetails
    {
        public int Id { get; set; }
        public Guid GUID { get; set; } = Guid.NewGuid();
        public string CreditNoteReferenceNumber { get; set; }
        public string InvoiceReferenceNumber { get; set; }
        public string Category { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public decimal ItemQuantity { get; set; }
        public int ItemAmount { get; set; }
        public int VAT { get; set; }
        public decimal UnitRate { get; set; }
        public decimal Discount { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal NatValue { get; set; }
        public DateTime CreatedDate { get; set; }

        public string CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }




    }

}
