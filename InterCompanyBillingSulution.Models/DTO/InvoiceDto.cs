using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class InvoiceDto
    {
        public string? CompanyId { get; set; }

        public string InvoiceReferenceNumber { get; set; }
        public string? QuotationReferenceNumber { get; set; }

        public string? invoiceId { get; set; }

        public DateTime invoiceDate { get; set; }
        public string? receivingEntity { get; set; }

        public decimal? subtotal { get; set; }

        public decimal? taxAmount { get; set; }

        public decimal? totalAmount { get; set; }
        public string? CompanyLogo { get; set; }
        //[JsonPropertyName("amount")]
        //public decimal? amount { get; set; }
        public string? quotationType { get; set; }
        public string CompanyName { get; set; }
        public string VatNumber { get; set; }
        public string RegistrationNumber { get; set; }
        public string CompanyAddress { get; set; }
        public string? CompanyWebsite { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhoneNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerAddress { get; set; }
        public string BankAccountNumber { get; set; }
        public string BranchCode { get; set; }
        public string BrannchAddress { get; set; }
        public string? IFSCCode { get; set; }
        public string? QuotationLink { get; set; }
        public string ReferenceNumber { get; set; }
        public string? PaymentTerms { get; set; }
        public string? customerRefNo { get; set; }
        public string? AccountHolderName { get; set; }

        public string? BankName { get; set; }
        [JsonPropertyName("dueDate")]

        public DateTime DueDate { get; set; }

        [JsonPropertyName("invoiceDetails")]
        public List<InvoiceDetailDto> invoiceDetails { get; set; }
    }
    public class InvoiceDetailDto
    {
        [JsonPropertyName("description")]
        public string? description { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? quantity { get; set; }

        [JsonPropertyName("price")]
        public decimal? price { get; set; }

        [JsonPropertyName("tax")]
        public int tax { get; set; }

        [JsonPropertyName("amount")]
        public decimal? amount { get; set; }
        public string? itemName { get; set; }

        public string? FileName { get; set; }
        public string? FilePath { get; set; }

        public string? ItemCode { get; set; }

        public decimal? Discount { get; set; }
        public string? category { get; set; }
        public string? InventoryId { get; set; }
        public string? DiscountType { get; set; }
        public string? CompanyId { get; set; }
        public decimal? NetAmount { get; set; }
        public decimal? DiscountValue { get; set; }

    }
    public class UpdateProformaDto
    {
        public string InvoiceReferenceNumber { get; set; }
        public string ProformaReferenceNumber { get; set; }
        public string Status { get; set; }
        public string? ModifiedBy { get; set; }

        public string? Reason { get; set; }
        public string? UnpaidStatus { get; set; }

        public string? TemplateUrl { get; set; }
    }

}
