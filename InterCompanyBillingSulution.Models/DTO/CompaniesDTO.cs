using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace InterCompanyBillingSulution.Models.DTO
{
    public class CompaniesDTO
    {
        public int Id { get; set; }
        public string? CompanyName { get; set; }
        public string? VatNumber { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? CompanyAddress { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? CompanyEmail { get; set; }
        public string? CompanyPhoneNumber { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchAddress { get; set; }
        public string? IFSCCode { get; set; }
        public string? AccountHolderName { get; set; }
        public string? CompanyLogo { get; set; }
        public string? Status { get; set; }
        public string? BankName { get; set; }
    }

}