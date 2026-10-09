using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{

    public class QuotationAudit
    {
        public int Id { get; set; }
        public int QuotationId { get; set; }

        public Guid GUID { get; set; } = Guid.NewGuid();
        [Required]
        public string ReferenceNumber { get; set; }
        [Required]
        public DateTime QuotationDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? PorformaReferenceNumber { get; set; }
        public string CompanyId { get; set; }
        public string QuotationType { get; set; }
        public string ReceivingEntity { get; set; }
        public string? Currency { get; set; }
        public string? Filename { get; set; }
        public string CompanyName { get; set; }
        public string VatNumber { get; set; }
        public string RegistrationNumber { get; set; }
        public string CompanyAddress { get; set; }
        public string? CompanyWebsite { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhoneNumber { get; set; }
        public string CustomerRefno { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerAddress { get; set; }
        public string AccountHolderName { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public string BranchCode { get; set; }
        public string BrannchAddress { get; set; }
        public string? IFSCCode { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string PaymentTerms { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public string Action { get; set; }
        public DateTime ActionDate { get; set; }
        public string ActionBy { get; set; }
        public string? IPaddressOfSystem { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
        public string? UnpaidStatus { get; set; }



    }
}
