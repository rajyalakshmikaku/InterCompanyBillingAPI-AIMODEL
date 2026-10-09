using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{

    public class UsersAudit
    {
        public int Id { get; set; }
        public Guid GUID { get; set; } = Guid.NewGuid();
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string SurName { get; set; }
        public string Email { get; set; }
        public int CompanyName { get; set; }
        public string Title { get; set; }
        public string Gender { get; set; }
        public string Activation { get; set; }
        public string? CustomerAccountNo { get; set; }
        public string Address { get; set; }
        public string Mobile { get; set; }
        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ActionBy { get; set; }
        public DateTime ActionDate { get; set; }
        public string Action { get; set; }
        public string IPaddressOfSystem { get; set; }

    }

}
