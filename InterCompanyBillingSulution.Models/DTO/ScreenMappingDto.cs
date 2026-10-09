using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{

    public class ScreenMappingDto
    {
        public int RoleId { get; set; }
        public List<int> ScreenIds { get; set; }
    }

}
