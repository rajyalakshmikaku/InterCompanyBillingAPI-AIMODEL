using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
   public class ProductsDTO
    {
        public int CompanyId { get; set; }
        public string Category { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string UnitOfMeasure { get; set; }
        public bool VatableStatus { get; set; }
        public int? MeterTypeId { get; set; }
        public string Status { get; set; }
    }
}
