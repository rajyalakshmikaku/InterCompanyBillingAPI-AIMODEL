using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
    public class Roles
    {
        public int Id { get; set; }
        public Guid GUID { get; set; } = Guid.NewGuid();

        public string RoleName { get; set; }

        public string RoleDescription { get; set; }

        public string Status { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
