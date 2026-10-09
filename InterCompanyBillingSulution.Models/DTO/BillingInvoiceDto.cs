using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class BillingInvoiceDetailDto
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

        public string? FileName { get; set; }
        public string? FilePath { get; set; }
    }

    public class BillingInvoiceDto
    {
        public string? CompanyId { get; set; }

        [JsonPropertyName("invoiceId")]
        public string invoiceId { get; set; }

        [JsonPropertyName("invoiceDate")]
        public DateTime invoiceDate { get; set; }

        [JsonPropertyName("billingEntity")]
        public string? billingEntity { get; set; }

        [JsonPropertyName("receivingEntity")]
        public string? receivingEntity { get; set; }

        [JsonPropertyName("currency")]
        public string? currency { get; set; }

        [JsonPropertyName("noteComment")]
        public string? noteComment { get; set; }

        [JsonPropertyName("projectCode")]
        public string? projectCode { get; set; }

        [JsonPropertyName("subtotal")]
        public decimal? subtotal { get; set; }

        [JsonPropertyName("taxAmount")]
        public decimal? taxAmount { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal? totalAmount { get; set; }

        [JsonPropertyName("invoiceDetails")]
        public List<BillingInvoiceDetailDto> InvoiceDetails { get; set; }
    }



}
