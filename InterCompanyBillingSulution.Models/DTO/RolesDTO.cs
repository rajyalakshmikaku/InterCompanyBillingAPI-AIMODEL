using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class RolesDTO
    {
        public int Id { get; set; }
        public string? RoleName { get; set; }
        public string? RoleDescription { get; set; }

        public string Status { get; set; }

        //public string? IsActive { get; set; }
        //public string? IsDeleted { get; set; }
    }
}
