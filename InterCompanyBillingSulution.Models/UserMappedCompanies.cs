using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
    public class UserMappedCompanies
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int CompanyId { get; set; }

        public string CompanyName { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}
