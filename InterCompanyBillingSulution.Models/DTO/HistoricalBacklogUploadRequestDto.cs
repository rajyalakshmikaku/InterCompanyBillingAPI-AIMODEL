using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace InterCompanyBillingSulution.Models.DTO
{
    public class HistoricalBacklogUploadRequestDto
    {
        public string? sourceName { get; set; }
        public string? sheetName { get; set; }
        public string? rowCount { get; set; }
        public string? rows { get; set; }
    }
}

