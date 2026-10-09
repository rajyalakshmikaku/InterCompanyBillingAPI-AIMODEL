using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
  
    public class CompaniesAudit
    {

        public int Id { get; set; }  
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyPhoneNumber { get; set; }
        public string CompanyEmail { get; set; }
        public string VatNumber { get; set; }
        public string RegistrationNumber { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? CompanyLogo { get; set; }
        public string? FilePath { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public string BranchCode { get; set; }
        public string BranchAddress { get; set; }
        public string? IFSCCode { get; set; }
        public string AccountHolderName { get; set; }
        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ActionBy { get; set; }
        public DateTime ActionDate { get; set; }
        public string Action { get; set; }
        public string? IPaddressOfSystem { get; set; }

    }

}
