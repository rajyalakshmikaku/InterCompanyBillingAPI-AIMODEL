using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
    public class ScreenMappingWithRole
    {
        public int Id { get; set; }
        public Guid GUID { get; set; } = Guid.NewGuid();
        public Nullable<int> RoleId { get; set; }
        public Nullable<int> ScreenId { get; set; }
        public string ScreenName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public string Status { get; set; }
        public string? Display { get; set; }

    }
}
