using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class ServiceRenderedDTO
    {
        public int? id { get; set; }
        public int companyId { get; set; }
        public string name { get; set; }
        public string? description { get; set; }
        public decimal? rate { get; set; }
        public string? unitOfMeasure { get; set; }
        public string? isActive { get; set; }
        public string? isDeleted { get; set; }


    }

}
