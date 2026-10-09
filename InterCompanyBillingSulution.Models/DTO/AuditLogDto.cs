using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class AuditLogDto
    {
        public string Table { get; set; }
        public string ActionBy { get; set; }
        public string Action { get; set; }
        public DateTime ActionDate { get; set; }
        public int Id { get; set; }
        public string Refno { get; set; }
    }
}
