using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }
        public Guid GUID { get; set; } = Guid.NewGuid();
        public string CompanyId { get; set; }
        public string Category { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string UnitOfMeasure { get; set; }
        public string? VAT { get; set; }
        public string? VatableStatus { get; set; }
        public string Status { get; set; }
        public string? MeterTypeId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }

    }
}
