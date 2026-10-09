using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class QutationInvioceDto
    {
        public int InvoiceId { get; set; }
        public int QuotationId { get; set; }

        public string DocumentType { get; set; }
        public string ReferenceNumber { get; set; }
        public string QuotationReferenceNumber { get; set; }
        public string InvoiceReferenceNumber { get; set; }

        public string ReceivingEntity { get; set; }

        public DateTime InvoiceDate { get; set; }
        public DateTime? QuotationDate { get; set; }
        public DateTime DueDate { get; set; }

        public string Status { get; set; }
        public string InvoiceStatus { get; set; }
        public string QuotationStatus { get; set; }

        public string Currency { get; set; }

        public string CompanyName { get; set; }
        public string CompanyWebsite { get; set; }
        public string CompanyLogo { get; set; }

        public string VatNumber { get; set; }
        public string RegistrationNumber { get; set; }

        public string CompanyEmail { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyPhoneNumber { get; set; }

        public string BankAccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public string BranchCode { get; set; }
        public string BranchAddress { get; set; }
        public string BankName { get; set; }
        public string AccountHolderName { get; set; }

        public string CustomerRefno { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPhone { get; set; }

        public string QuotationType { get; set; }
        public string PaymentTerms { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public string Reason { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string? PorformaReferenceNumber { get; set; }
        public string? Type { get; set; }
        public List<QuotationInvoiceDetailDto> Detail { get; set; } = new();
        public List<QuotationPaymentDto> PaymentHistory { get; set; } = new();

    }
}
