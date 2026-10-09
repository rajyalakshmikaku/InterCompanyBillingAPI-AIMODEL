using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    //public class MasterUpdateDTO
    //{
    //    public string EntityType { get; set; }
    //    public int Id { get; set; }

    //    // Currency fields
    //    public string CurrencyName { get; set; }

    //    // Tax fields
    //    public decimal? TaxPercentage { get; set; }
    //    public DateTime? StartDate { get; set; }
    //    public DateTime? EndDate { get; set; }

    //    // Status fields
    //    public string StatusName { get; set; }

    //    public string Description { get; set; }
    //}

    public class MasterUpdateDTO
    {
        public int Id { get; set; }
        public string? EntityType { get; set; }
        public string? QuotationType { get; set; }
        public string? CurrencyName { get; set; }
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
