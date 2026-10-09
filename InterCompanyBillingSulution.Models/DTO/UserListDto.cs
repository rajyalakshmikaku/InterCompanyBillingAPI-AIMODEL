using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class UserListDto
    {
            public int Id { get; set; }
            public string Name { get; set; }
            public string SurName { get; set; }
            public string Mobile { get; set; }
            public string Address { get; set; }
            public int? DepartmentId { get; set; }
            public string Title { get; set; }
            public string Gender { get; set; }
            public string Password { get; set; }
            public int RoleId { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            public string CustomerAccountNo { get; set; }
          public string IsActive { get; set; }
          public string IsDeleted { get; set; }
        //public int? CompanyId { get; set; }
          public string CompanyName { get; set; }
            public string RoleName { get; set; }
        }

 }

