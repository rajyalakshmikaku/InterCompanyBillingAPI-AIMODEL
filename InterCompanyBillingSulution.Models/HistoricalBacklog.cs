using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models
{
        public class HistoricalBacklog
        {

            // From Company
            public int Id { get; set; }
            public string? From_Company_Name { get; set; }
            public string? From_Address { get; set; }
            public string? From_Phone_Number { get; set; }
            public string? From_Email { get; set; }
            public string? From_VAT_Number { get; set; }
            public string? From_Registration_Number { get; set; }

            // To Company
            public string? To_Company_Name { get; set; }
            public string? To_Address { get; set; }
            public string? To_Phone_Number { get; set; }
            public string? To_Email { get; set; }
            public string? To_VAT_Number { get; set; }
            public string? To_Registration_Number { get; set; }

            // Customer
            public string? Title { get; set; }
            public string? Name { get; set; }
            public string? Surname { get; set; }
            public string? Email { get; set; }
            public string? Role { get; set; }
            public string? Gender { get; set; }
            public string? Address { get; set; }
            public string? Mobile_Number { get; set; }
            public string? Username { get; set; }

            // Item
            public string Item_Name { get; set; }
            public string Description { get; set; }
            public string Currency { get; set; }
            public string Price { get; set; }
            public string Unit_Rate_Per_Item { get; set; }
            public string Quantity { get; set; }
            public string? Tax_Percentage { get; set; }

            // Quotation
            public string? Reference_Number { get; set; }
            public string? Quotation_Date { get; set; }
            public string? QuotationType { get; set; }

            // Bank Details
            public string? Bank_Account_Number { get; set; }
            public string? Bank_Name { get; set; }
            public string? Branch_Code { get; set; }
            public string? Branch_Address { get; set; }
            public string? Bank_Account_Holder_Name { get; set; }

            // Amounts
            public string? Subtotal { get; set; }
            public string? Tax_Amount { get; set; }
            public string? Total_Amount { get; set; }

            // Invoice
            public string? Invoice_Reference_Number { get; set; }
            public string? Invoice_Date { get; set; }

            // Payment
            public string? Payment_Terms { get; set; }

            // Unknown column
            public string? column43 { get; set; }
        }

    
}

