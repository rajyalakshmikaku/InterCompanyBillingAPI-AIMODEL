using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
    public class QuotationInvoice
    {

        public int Id { get; set; }
        public Guid GUID { get; set; } = Guid.NewGuid();
        public string InvoiceReferenceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string QuotationReferenceNumber { get; set; }
        public string? PorformaReferenceNumber { get; set; }
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string VatNumber { get; set; }
        public string RegistrationNumber { get; set; }
        public string CompanyAddress { get; set; }
        public string? CompanyWebsite { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhoneNumber { get; set; }
        public string? Filename { get; set; }
        public string BankName { get; set; }
        public string AccountHolderName { get; set; }
        public string BankAccountNumber { get; set; }
        public string BranchCode { get; set; }
        public string BranchAddress { get; set; }
        public string? IFSCCode { get; set; }
        public string QuotationType { get; set; }
        public string ReceivingEntity { get; set; }
        public string Currency { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerRefno { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string? CancelledBy { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string PaymentTerms { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }


    }
}
