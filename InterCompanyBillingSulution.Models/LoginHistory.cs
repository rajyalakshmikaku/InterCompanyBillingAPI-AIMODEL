using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
    public class LoginHistory
    {

        public int Id { get; set; }
        public Guid GUID { get; set; } = Guid.NewGuid();
        public int? UserID { get; set; }
        public string UserName { get; set; }
        public string LoginStatus { get; set; }
        public string? IPaddressOfSystem { get; set; }
        public DateTime LoginDateTime { get; set; }
        public string IsLocked { get; set; }
    }

}
