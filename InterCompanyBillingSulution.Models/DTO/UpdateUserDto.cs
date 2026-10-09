using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class UpdateUserDto
    {
        public int Id { get; set; } // User ID
        public string Name { get; set; }
        public string SurName { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }
        public string Title { get; set; }
        public string Gender { get; set; }
        public int RoleId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public int CompanyName { get; set; }
        public string Activation { get; set; }
        public List<int> CompanyIds { get; set; } = new();
    }

}
