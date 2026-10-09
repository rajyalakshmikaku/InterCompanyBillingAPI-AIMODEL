using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
    public class PaymentUploads
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; }
        
        public string Filename { get; set; }

        public string Filepath { get; set; }

        public string Type { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; }

        public DateTime ModifiedDate { get; set; }

    }
}
