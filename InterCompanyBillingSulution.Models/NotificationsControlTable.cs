using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
    public class NotificationsControlTable
    {
        public int Id { get; set; }
        public required string NotificationType { get; set; }
        public required string Status { get; set; }
        
        
    }
}
