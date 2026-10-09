using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{

    public class CreditNoteHeader
    {
        public int Id { get; set; }
        public Guid GUID { get; set; } = Guid.NewGuid();
        public string CreditNoteReferenceNumber { get; set; }
        public DateTime CreditNoteDate { get; set; }
        public DateTime DueDate { get; set; }
        public string InoviceReferenceNumber { get; set; }
        public string QuotationType { get; set; }
        public string Currency { get; set; }
        public string CompanyName { get; set; }
        public string VatNumber { get; set; }
        public string RegistrationNumber { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyWebsite { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhoneNumber { get; set; }
        public string BankAccountNumber { get; set; }
        public string BranchCode { get; set; }
        public string BrannchAddress { get; set; }
        public string IFSCCode { get; set; }
        public string AccountHolderName { get; set; }
        public string PaymentTerms { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerRefno { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }

    }

}
