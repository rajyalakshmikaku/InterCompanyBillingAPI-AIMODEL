using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{

    public class MasterSaveDTO
    {
        public string? EntityType { get; set; }
        public string? CurrencyName { get; set; }
        public string? Typename { get; set; }
        public decimal? TaxPercentage { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? StatusName { get; set; }
        public string? Description { get; set; }
        public string? CategoryName { get; set; }
        public string? Status { get; set; }
        public string? ScreenName { get; set; }
    }

}
