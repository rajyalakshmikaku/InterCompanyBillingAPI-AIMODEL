using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class CustomersDTO
    {
        public int Id { get; set; }
        public string? title { get; set; }
        public string? name { get; set; }
        public string? surname { get; set; }
        public required string email { get; set; }
        public int companyId { get; set; }
        public string? gender { get; set; }
        public string? CustomerRefNo { get; set; }
        public string? address { get; set; }
        public string? mobile { get; set; }
        public string? Status { get; set; }
     
     

    }

}
