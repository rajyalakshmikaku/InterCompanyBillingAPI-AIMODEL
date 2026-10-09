using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class InventoryItemDto
    {
        public string ItemName { get; set; }

        public string ItemCode { get; set; }

        public string Description { get; set; }

        public string Category { get; set; }

        public decimal Price { get; set; }

        public string UnitOfMeasure { get; set; }

        // You are using CompanyId in UI but mapping to Discount in DB
        public int CompanyId { get; set; }

        // true / false from CSV
        public bool VatableStatus { get; set; }
    }
}
