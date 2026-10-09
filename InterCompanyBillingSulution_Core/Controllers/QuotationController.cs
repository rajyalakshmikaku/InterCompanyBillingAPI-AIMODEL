using InterCompanyBillingSulution.DataAcess.Data;
using InterCompanyBillingSulution.Models.DTO;
using InterCompanyBillingSulution.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Azure.Core;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Transactions;
using Microsoft.VisualBasic;

namespace InterCompanyBillingSulution_Core.Controllers
{

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class QuotationController : ControllerBase
    {
        private readonly InterCompanyBillingDbContext _context;
        private readonly IEmailService _emailService;
        private readonly AuditService _auditService;

        public QuotationController(InterCompanyBillingDbContext context, IEmailService emailService, AuditService auditService)
        {
            _context = context;
            _emailService = emailService;
            _auditService = auditService;

        }
        [HttpGet("GetQuotationRefno_old")]
        public async Task<IActionResult> GetQuotationRefno_old()
        {
            //  var company = await _context.Companies
            //.FirstOrDefaultAsync(i => i.Id == companyId);

            //  if (company == null)
            //  {
            //      return NotFound("Company not found");
            //  }

            string datePart = DateTime.Now.ToString("yyyy-MMdd");
            string prefixWithDate = $"QUO-{datePart}-";

            // Get last invoice ONLY for today's prefix
            var lastInvoice = await _context.BillingQuotations
                .Where(i => i.ReferenceNumber.StartsWith(prefixWithDate))
                .OrderByDescending(i => i.ReferenceNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastInvoice != null)
            {
                string[] parts = lastInvoice.ReferenceNumber.Split('-');
                if (parts.Length == 4 && int.TryParse(parts[3], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            string newRefNo = $"QUO-{datePart}-{nextNumber:D4}";


            return Ok(new
            {
                referenceNumber = newRefNo,
                //companyname = company.CompanyName
            });
        }

        private async Task<string> GenerateQuotationReferenceNumber()
        {
            var lastQuotation = await _context.BillingQuotations
                .Where(x => x.ReferenceNumber.StartsWith("QUO-"))
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastQuotation != null && !string.IsNullOrEmpty(lastQuotation.ReferenceNumber))
            {
                var parts = lastQuotation.ReferenceNumber.Split('-');

                // Expected: QUO-2026-0819-0001
                if (parts.Length == 4 && int.TryParse(parts[3], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            string datePart = DateTime.Now.ToString("yyyy-MMdd");

            return $"QUO-{datePart}-{nextNumber:D4}";
        }
        [HttpGet("getQuotationRefno")]
        public async Task<IActionResult> GetQuotationRefno()
        {
            try
            {
                string newRefNo = await GenerateQuotationReferenceNumber();

                return Ok(new
                {
                    success = true,
                    referenceNumber = newRefNo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generating reference number",
                    error = ex.Message
                });
            }
        }
        [HttpPost("saveQuotation")]
        public async Task<IActionResult> SaveQuotation([FromForm] string invoiceData)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                int.TryParse(userIdClaim, out loggedUserId);
            }

            if (string.IsNullOrWhiteSpace(invoiceData))
                return BadRequest("Missing invoice data.");

            BillingQuotationDTO invoiceDto;

            try
            {
                invoiceDto = JsonSerializer.Deserialize<BillingQuotationDTO>(invoiceData);
            }
            catch
            {
                return BadRequest("Invalid JSON format.");
            }

            if (invoiceDto == null || invoiceDto.quotationDetails == null || !invoiceDto.quotationDetails.Any())
                return BadRequest("Quotation Details are required.");


            try
            {

                //Convert quotation details into JSON
                string quotationDetailsJson = JsonSerializer.Serialize(invoiceDto.quotationDetails);

                using (SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_SaveQuotation", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@QuotationDate", invoiceDto.quotationDate);
                        cmd.Parameters.AddWithValue("@ReceivingEntity", invoiceDto.receivingEntity ?? "");
                        cmd.Parameters.AddWithValue("@Currency", invoiceDto.currency ?? "");
                        cmd.Parameters.AddWithValue("@QuotationType", invoiceDto.quotationType ?? "");

                        cmd.Parameters.AddWithValue("@CompanyName", invoiceDto.CompanyName ?? "");
                        cmd.Parameters.AddWithValue("@VatNumber", invoiceDto.VatNumber ?? "");
                        cmd.Parameters.AddWithValue("@RegistrationNumber", invoiceDto.RegistrationNumber ?? "");
                        cmd.Parameters.AddWithValue("@CompanyAddress", invoiceDto.CompanyAddress ?? "");
                        cmd.Parameters.AddWithValue("@CompanyWebsite", invoiceDto.CompanyWebsite ?? "");
                        cmd.Parameters.AddWithValue("@CompanyEmail", invoiceDto.CompanyEmail ?? "");
                        cmd.Parameters.AddWithValue("@CompanyPhoneNumber", invoiceDto.CompanyPhoneNumber ?? "");

                        cmd.Parameters.AddWithValue("@CustomerName", invoiceDto.CustomerName ?? "");
                        cmd.Parameters.AddWithValue("@CustomerAddress", invoiceDto.CustomerAddress ?? "");
                        cmd.Parameters.AddWithValue("@CustomerEmail", invoiceDto.CustomerEmail ?? "");
                        cmd.Parameters.AddWithValue("@CustomerPhone", invoiceDto.CustomerPhone ?? "");

                        cmd.Parameters.AddWithValue("@BankAccountNumber", invoiceDto.BankAccountNumber ?? "");
                        cmd.Parameters.AddWithValue("@BrannchAddress", invoiceDto.BrannchAddress ?? "");
                        cmd.Parameters.AddWithValue("@BranchCode", invoiceDto.BranchCode ?? "");
                        cmd.Parameters.AddWithValue("@IFSCCode", invoiceDto.IFSCCode ?? "");

                        cmd.Parameters.AddWithValue("@Filename", invoiceDto.CompanyLogo ?? "");

                        cmd.Parameters.AddWithValue("@SubTotal", invoiceDto.subtotal);
                        cmd.Parameters.AddWithValue("@TaxAmount", invoiceDto.taxAmount);
                        cmd.Parameters.AddWithValue("@TotalAmount", invoiceDto.totalAmount);

                        cmd.Parameters.AddWithValue("@PaymentTerms", invoiceDto.PaymentTerms ?? "");
                        cmd.Parameters.AddWithValue("@CustomerRefNo", invoiceDto.customerRefNo ?? "");
                        cmd.Parameters.AddWithValue("@AccountHolderName", invoiceDto.AccountHolderName ?? "");
                        cmd.Parameters.AddWithValue("@CompanyId", invoiceDto.CompanyId);
                        cmd.Parameters.AddWithValue("@BankName", invoiceDto.BankName ?? "");
                        cmd.Parameters.AddWithValue("@CreatedBy", loggedUserId);
                        cmd.Parameters.AddWithValue("@DueDate", invoiceDto.dueDate);

                        //Quotation Details JSON
                        cmd.Parameters.AddWithValue("@QuotationDetails", quotationDetailsJson);

                        await con.OpenAsync();

                        int quotationId = 0;
                        int success = 0;
                        string message = "";
                        string ReferenceNumber = "";
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                success = Convert.ToInt32(reader["Success"]);
                                message = reader["Message"].ToString();

                                if (success == 1)
                                {
                                    quotationId = Convert.ToInt32(reader["QuotationId"]);
                                    ReferenceNumber = reader["ReferenceNumber"]?.ToString() ?? "";



                                }
                            }
                        }

                        if (success == 0)
                        {
                            return Ok(new
                            {
                                success = false,
                                message = message
                            });
                        }

                        //----------------------------------------------------
                        // Audit
                        //----------------------------------------------------

                        var quotation = await _context.BillingQuotations
                            .FirstOrDefaultAsync(x => x.Id == quotationId);

                        if (quotation != null)
                        {
                            await _auditService.AddQuotationAudit(quotation, "Quotation Created Successfully");
                        }

                        //----------------------------------------------------
                        // Send Emails
                        //----------------------------------------------------

                        var customerUrl = $"{invoiceDto.QuotationLink}?quotationId={quotation.ReferenceNumber}&guid={quotation.GUID}&type=customer";
                        var companyUrl = $"{invoiceDto.QuotationLink}?quotationId={quotation.ReferenceNumber}&type=company";

                        var customerEmail = new EmailMessage
                        {
                            ToEmail = invoiceDto.CustomerEmail,
                            Subject = "Quotation Sent Action-Required",
                            Body = $@"
                        <p>Dear {invoiceDto.CustomerName},</p>

                        <p>Attached is your quotation for review.</p>

                        <p>
                        Please click
                        <a href='{customerUrl}'>here</a>
                        to review and Accept/Decline your quotation.
                        </p>

                        <br/>

                        <p>Kind regards,<br/>ITSA Support Team</p>",
                            IsBodyHtml = true
                        };

                        await _emailService.SendEmailAsync(customerEmail);

                        /*
                        var companyEmail = new EmailMessage
                        {
                            ToEmail = invoiceDto.CompanyEmail,
                            Subject = "Quotation Sent Action-Required",
                            Body = $@"
                            <p>Dear {invoiceDto.CompanyName},</p>

                            <p>Quotation has been created.</p>

                            <a href='{companyUrl}'>View Quotation</a>

                            <p>Kind regards,<br/>ITSA Support Team</p>",
                            IsBodyHtml = true
                        };

                        await _emailService.SendEmailAsync(companyEmail);
                        */

                        return Ok(new
                        {
                            success = true,
                            quotationId,
                            ReferenceNumber,
                            message
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpGet("getQuotationList")]
        public async Task<IActionResult> GetInvoiceList()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            var loggedRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            int? loggedCompanyId = null;
            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;

            if (int.TryParse(companyIdClaim, out var companyId))
                loggedCompanyId = companyId;

            List<dynamic> quotations = new();
            List<dynamic> details = new();
            List<dynamic> payments = new();

            using (SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetQuotationList", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Role", loggedRole ?? "");
                    cmd.Parameters.AddWithValue("@UserId", loggedUserId);

                    if (loggedCompanyId.HasValue)
                        cmd.Parameters.AddWithValue("@CompanyId", loggedCompanyId.Value);
                    else
                        cmd.Parameters.AddWithValue("@CompanyId", DBNull.Value);

                    await con.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        // First Result Set (Quotation Header)
                        while (await reader.ReadAsync())
                        {
                            quotations.Add(new
                            {
                                QuotationId = reader["QuotationId"],
                                ReferenceNumber = reader["ReferenceNumber"],
                                ReceivingEntity = reader["ReceivingEntity"],
                                QuotationDate = reader["QuotationDate"],
                                Currency = reader["Currency"],
                                QuotationType = reader["QuotationType"],
                                Status = reader["Status"],
                                CompanyName = reader["CompanyName"],
                                VatNumber = reader["VatNumber"],
                                RegistrationNumber = reader["RegistrationNumber"],
                                CompanyAddress = reader["CompanyAddress"],
                                CompanyWebsite = reader["CompanyWebsite"],
                                CompanyEmail = reader["CompanyEmail"],
                                CompanyPhoneNumber = reader["CompanyPhoneNumber"],
                                CompanyLogo = reader["CompanyLogo"],
                                CustomerName = reader["CustomerName"],
                                CustomerPhone = reader["CustomerPhone"],
                                CustomerEmail = reader["CustomerEmail"],
                                CustomerAddress = reader["CustomerAddress"],
                                BankAccountNumber = reader["BankAccountNumber"],
                                BranchCode = reader["BranchCode"],
                                BrannchAddress = reader["BrannchAddress"],
                                IFSCCode = reader["IFSCCode"],
                                AccountHolderName = reader["AccountHolderName"],
                                PaymentTerms = reader["PaymentTerms"],
                                TaxAmount = reader["TaxAmount"],
                                TotalAmount = reader["TotalAmount"],
                                SubTotal = reader["SubTotal"],
                                CompanyId = reader["CompanyId"],
                                Reason = reader["Reason"] == DBNull.Value ? null : reader["Reason"].ToString(),
                                BankName = reader["BankName"],
                                DueDate = reader["DueDate"],
                                PorformaReferenceNumber = reader["PorformaReferenceNumber"] == DBNull.Value ? null : reader["PorformaReferenceNumber"].ToString()
                            });
                        }

                        // Second Result Set (Details)
                        await reader.NextResultAsync();

                        while (await reader.ReadAsync())
                        {
                            details.Add(new
                            {
                                ReferenceNumber = reader["ReferenceNumber"].ToString(),
                                DetailId = reader["DetailId"],
                                QuotationAmount = reader["QuotationAmount"],
                                QuotationQuantity = reader["QuotationQuantity"],
                                UnitRate = reader["UnitRate"],
                                InventoryId = reader["InventoryId"],
                                Description = reader["Description"],
                                Category = reader["Category"],
                                ItemCode = reader["ItemCode"],
                                ItemName = reader["ItemName"],
                                Discount = reader["Discount"],
                                DiscountType = reader["DiscountType"],
                                Tax = reader["VAT"],
                                DiscountValue = reader["DiscountValue"]
                            });
                        }
                        await reader.NextResultAsync();

                        // Third Result Set (Payment History)

                        while (await reader.ReadAsync())
                        {
                            payments.Add(new
                            {
                                PaymentId =
                                    reader["PaymentId"] == DBNull.Value
                                        ? 0
                                        : Convert.ToInt32(reader["PaymentId"]),

                                QuotationRefNo =
                                    reader["QuotationRefNo"] == DBNull.Value
                                        ? null
                                        : reader["QuotationRefNo"].ToString(),

                                TransactionId =
                                    reader["TransactionId"] == DBNull.Value
                                        ? null
                                        : reader["TransactionId"].ToString(),

                                PaymentDate =
                                    reader["PaymentDate"] == DBNull.Value
                                        ? (DateTime?)null
                                        : Convert.ToDateTime(reader["PaymentDate"]),

                                PaidAmount =
                                    reader["PaidAmount"] == DBNull.Value
                                        ? 0
                                        : Convert.ToDecimal(reader["PaidAmount"]),

                                PaymentMethod =
                                    reader["PaymentMethod"] == DBNull.Value
                                        ? null
                                        : reader["PaymentMethod"].ToString(),

                                TotalAmount =
                                    reader["TotalAmount"] == DBNull.Value
                                        ? 0
                                        : Convert.ToDecimal(reader["TotalAmount"]),

                                TotatlDue =
                                    reader["TotatlDue"] == DBNull.Value
                                        ? 0
                                        : Convert.ToDecimal(reader["TotatlDue"])
                            });
                        }
                    }
                }

                var result = quotations.Select(q => new
                {
                    q.QuotationId,
                    q.ReferenceNumber,
                    q.ReceivingEntity,
                    q.QuotationDate,
                    q.Currency,
                    q.QuotationType,
                    q.Status,
                    q.CompanyName,
                    q.VatNumber,
                    q.RegistrationNumber,
                    q.CompanyAddress,
                    q.CompanyWebsite,
                    q.CompanyEmail,
                    q.CompanyPhoneNumber,
                    q.CompanyLogo,
                    q.CustomerName,
                    q.CustomerPhone,
                    q.CustomerEmail,
                    q.CustomerAddress,
                    q.BankAccountNumber,
                    q.BranchCode,
                    q.BrannchAddress,
                    q.IFSCCode,
                    q.AccountHolderName,
                    q.PaymentTerms,
                    q.TaxAmount,
                    q.TotalAmount,
                    q.SubTotal,
                    q.CompanyId,
                    q.Reason,
                    q.BankName,
                    q.DueDate,
                    q.PorformaReferenceNumber,
                    Details = details.Where(d => d.ReferenceNumber == q.ReferenceNumber).ToList(),
                    Payments = payments.Where(p => p.QuotationRefNo == q.ReferenceNumber).ToList()
                });

                return Ok(new { list = result });
            }
        }

        //[HttpGet("getQuotationList")]
        //public async Task<IActionResult> GetInvoiceList()
        //{
        //    var loggedRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        //    int? loggedCompanyId = null;
        //    var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;

        //    if (int.TryParse(companyIdClaim, out var companyId))
        //        loggedCompanyId = companyId;

        //    List<QuotationDto> quotations = new();
        //    List<QuotationDetailDto> details = new();

        //    using (SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
        //    {
        //        using (SqlCommand cmd = new SqlCommand("sp_GetQuotationList", con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            cmd.Parameters.AddWithValue("@Role", loggedRole ?? "");

        //            if (loggedCompanyId.HasValue)
        //                cmd.Parameters.AddWithValue("@CompanyId", loggedCompanyId.Value);
        //            else
        //                cmd.Parameters.AddWithValue("@CompanyId", DBNull.Value);

        //            await con.OpenAsync();

        //            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        //            {
        //                // Header
        //                while (await reader.ReadAsync())
        //                {
        //                    quotations.Add(new QuotationDto
        //                    {
        //                        QuotationId = reader["QuotationId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["QuotationId"]),
        //                        QuotationReferenceNumber = reader["ReferenceNumber"]?.ToString()?.Trim(),
        //                        ReceivingEntity = reader["ReceivingEntity"]?.ToString(),
        //                        QuotationDate = reader["QuotationDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["QuotationDate"]),
        //                        DueDate = reader["DueDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DueDate"]),
        //                        Currency = reader["Currency"]?.ToString(),
        //                        QuotationType = reader["QuotationType"]?.ToString(),
        //                        QuotationStatus = reader["Status"]?.ToString(),
        //                        CompanyName = reader["CompanyName"]?.ToString(),
        //                        VatNumber = reader["VatNumber"]?.ToString(),
        //                        RegistrationNumber = reader["RegistrationNumber"]?.ToString(),
        //                        CompanyAddress = reader["CompanyAddress"]?.ToString(),
        //                        CompanyWebsite = reader["CompanyWebsite"]?.ToString(),
        //                        CompanyEmail = reader["CompanyEmail"]?.ToString(),
        //                        CompanyPhoneNumber = reader["CompanyPhoneNumber"]?.ToString(),
        //                        CompanyLogo = reader["CompanyLogo"]?.ToString(),
        //                        CustomerName = reader["CustomerName"]?.ToString(),
        //                        CustomerPhone = reader["CustomerPhone"]?.ToString(),
        //                        CustomerEmail = reader["CustomerEmail"]?.ToString(),
        //                        CustomerAddress = reader["CustomerAddress"]?.ToString(),
        //                        BankAccountNumber = reader["BankAccountNumber"]?.ToString(),
        //                        BranchCode = reader["BranchCode"]?.ToString(),
        //                        BranchAddress = reader["BrannchAddress"]?.ToString(),
        //                        IFSCCode = reader["IFSCCode"]?.ToString(),
        //                        AccountHolderName = reader["AccountHolderName"]?.ToString(),
        //                        PaymentTerms = reader["PaymentTerms"]?.ToString(),
        //                        TaxAmount = reader["TaxAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TaxAmount"]),
        //                        TotalAmount = reader["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalAmount"]),
        //                        SubTotal = reader["SubTotal"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SubTotal"]),
        //                        CompanyId = reader["CompanyId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CompanyId"]),
        //                        Reason = reader["Reason"]?.ToString(),
        //                        BankName = reader["BankName"]?.ToString(),
        //                        Detail = new List<QuotationDetailDto>()
        //                    });
        //                }

        //                // Read second result set
        //                if (await reader.NextResultAsync())
        //                {
        //                    while (await reader.ReadAsync())
        //                    {
        //                        details.Add(new QuotationDetailDto
        //                        {
        //                            DetailReferenceNumber = reader["ReferenceNumber"]?.ToString()?.Trim(),
        //                            DetailId = reader["DetailId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DetailId"]),
        //                            QuotationAmount = reader["QuotationAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QuotationAmount"]),
        //                            QuotationQuantity = reader["QuotationQuantity"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QuotationQuantity"]),
        //                            UnitRate = reader["UnitRate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["UnitRate"]),
        //                            InventoryId = reader["InventoryId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InventoryId"]),
        //                            Description = reader["Description"]?.ToString(),
        //                            Category = reader["Category"]?.ToString(),
        //                            ItemCode = reader["ItemCode"]?.ToString(),
        //                            ItemName = reader["ItemName"]?.ToString(),
        //                            Discount = reader["Discount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Discount"]),
        //                            DiscountType = reader["DiscountType"]?.ToString(),
        //                            Tax = reader["Tax"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Tax"])
        //                        });
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    // Map Details to Header
        //    foreach (var quotation in quotations)
        //    {
        //        quotation.Detail = details
        //            .Where(d => string.Equals(
        //                d.DetailReferenceNumber?.Trim(),
        //                quotation.QuotationReferenceNumber?.Trim(),
        //                StringComparison.OrdinalIgnoreCase))
        //            .ToList();
        //    }
        //    return Ok(new
        //    {
        //        list = quotations
        //    });
        //}
        [HttpGet("getCustomersList")]
        public async Task<IActionResult> GetCustomersList(int excludeIds)
        {
            var loggedRole =
                User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            bool isSuperAdmin = loggedRole?.Trim().ToLower() == "super admin" || loggedRole?.Trim().ToLower() == "superadmin" ;


            var userIdClaim =
                User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            if (!int.TryParse(userIdClaim, out int loggedUserId))
                return Unauthorized("Invalid user");


            // ============================================================
            // CUSTOMER + COMPANY QUERY
            // ============================================================

            var customersQuery =
                from u in _context.Customers
                join c in _context.Companies
                    on u.CompanyId equals c.Id
                where u.Status == "Active" && c.Status == "Active"
                select new
                {
                    Customer = u,
                    Company = c
                };


            // ============================================================
            // NON-SUPER ADMIN
            // ONLY MAPPED COMPANIES
            // ============================================================

            //if (!isSuperAdmin)
            //{
            //    var mappedCompanyIds = _context.UserMappedCompanies
            //        .Where(x => x.UserId == loggedUserId)
            //        .Select(x => x.CompanyId);

            //    customersQuery = customersQuery
            //        .Where(x =>
            //            mappedCompanyIds.Contains(x.Customer.CompanyId));
            //}


            // ============================================================
            // GET CUSTOMERS
            // ============================================================

            var customersList = await customersQuery
                .Select(x => new
                {
                    x.Customer.Id,
                    x.Customer.UserName,
                    x.Customer.Email,
                    x.Customer.Mobile,
                    x.Customer.Address,
                    x.Customer.CustomerRefNo,
                    x.Customer.Name,

                    receivingCompany = x.Company.CompanyName,

                    x.Company.CompanyAddress,
                    x.Company.CompanyEmail,
                    x.Company.CompanyLogo,
                    x.Company.CompanyPhoneNumber,
                    x.Company.VatNumber,
                    x.Company.RegistrationNumber,
                    x.Company.AccountHolderName,
                    x.Company.BranchCode,
                    x.Company.BankAccountNumber,
                    x.Company.BranchAddress,
                    x.Company.IFSCCode
                })
                .ToListAsync();


            return Ok(new
            {
                list = customersList
            });
        }
        [HttpGet("SearchCompany")]

        public async Task<IActionResult> SearchCompany()
        {
            var loggedRole =
                User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            bool isSuperAdmin = loggedRole?.Trim().ToLower() == "super admin" || loggedRole?.Trim().ToLower() == "superadmin" || loggedRole?.Trim().ToLower() == "admin";

            var userIdClaim =
                User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            if (!int.TryParse(userIdClaim, out int loggedUserId))
            {
                return Unauthorized(new
                {
                    message = "Invalid user"
                });
            }

            IQueryable<Companies> companyQuery =
                _context.Companies.Where(x => x.Status == "Active");


            // ============================================================
            // NON-SUPER ADMIN
            // ============================================================

            if (!isSuperAdmin)
            {
                var mappedCompanyIds = _context.UserMappedCompanies
                    .Where(x => x.UserId == loggedUserId)
                    .Select(x => x.CompanyId);

                companyQuery = companyQuery
                    .Where(c => mappedCompanyIds.Contains(c.Id));
            }


            // ============================================================
            // GET COMPANIES
            // ============================================================

            var companies = await companyQuery
                .OrderBy(x => x.CompanyName)
                .ToListAsync();

            if (!companies.Any())
            {
                return NotFound(new
                {
                    message = "No company found"
                });
            }

            return Ok(companies);
        }

        [HttpGet("GetAvailableCompanies")]
        public async Task<IActionResult> GetAvailableCompanies([FromQuery] List<int> excludeIds)
        {
            // Start with all companies
            var query = _context.Companies.AsQueryable();

            // Exclude already selected companies
            if (excludeIds != null && excludeIds.Any())
            {


                query = query.Where(c => !excludeIds.Contains(c.Id));
            }

            //// Optional: apply search filter
            //if (!string.IsNullOrEmpty(searchQuery))
            //{
            //    query = query.Where(c =>
            //        c.CompanyName.Contains(searchQuery) ||
            //        c.CompanyPhoneNumber.Contains(searchQuery) ||
            //        c.CompanyEmail.Contains(searchQuery) ||
            //        c.CompanyAddress.Contains(searchQuery) ||
            //        c.RegistrationNumber.Contains(searchQuery) ||
            //        c.VatNumber.Contains(searchQuery) ||
            //        c.CompanyWebsite.Contains(searchQuery));
            //}

            var companies = await query.ToListAsync();

            if (!companies.Any())
                return NotFound("No companies found");

            return Ok(companies);
        }
        [HttpPost("update/{referenceNumber}")]
        public async Task<IActionResult> UpdateQuotation(string referenceNumber, [FromBody] BillingQuotationDTO quotationDto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            if (quotationDto == null)
                return BadRequest("Invalid data.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var quotation = await _context.BillingQuotations
                    .FirstOrDefaultAsync(q => q.ReferenceNumber == referenceNumber);

                if (quotation == null)
                    return NotFound("Quotation not found.");

                // ===== Check for changes =====
                bool parentChanged =
                    quotation.CompanyId != quotationDto.CompanyId ||
                    quotation.ReceivingEntity != quotationDto.receivingEntity ||
                    quotation.Currency != quotationDto.currency ||
                    quotation.PaymentTerms != quotationDto.PaymentTerms ||
                    quotation.SubTotal != (quotationDto.subtotal ?? 0) ||
                    quotation.TaxAmount != (quotationDto.taxAmount ?? 0) ||
                    quotation.TotalAmount != (quotationDto.totalAmount ?? 0) ||
                    quotation.QuotationType != quotationDto.quotationType ||
                    quotation.CompanyName != quotationDto.CompanyName ||
                    quotation.CompanyEmail != quotationDto.CompanyEmail ||
                    quotation.CompanyPhoneNumber != quotationDto.CompanyPhoneNumber ||
                    quotation.CompanyAddress != quotationDto.CompanyAddress ||
                    quotation.VatNumber != quotationDto.VatNumber ||
                    quotation.RegistrationNumber != quotationDto.RegistrationNumber ||
                    quotation.CompanyWebsite != quotationDto.CompanyWebsite ||
                    quotation.BankAccountNumber != quotationDto.BankAccountNumber ||
                    quotation.BranchCode != quotationDto.BranchCode ||
                    quotation.BrannchAddress != quotationDto.BrannchAddress ||
                    quotation.IFSCCode != quotationDto.IFSCCode ||
                    quotation.CustomerName != quotationDto.CustomerName ||
                    quotation.CustomerEmail != quotationDto.CustomerEmail ||
                    quotation.CustomerPhone != quotationDto.CustomerPhone ||
                     quotation.BankName != quotationDto.BankName ||
                quotation.AccountHolderName != quotationDto.AccountHolderName ||
                quotation.CustomerAddress != quotationDto.CustomerAddress ||
                quotation.DueDate != quotationDto.dueDate;

                // ===== Handle details =====
                var existingDetails = await _context.BillingQuotationDetails
                    .Where(d => d.ReferenceNumber == quotation.ReferenceNumber)
                    .ToListAsync();

                bool detailsChanged = false;

                // Check removed items
                foreach (var detail in existingDetails)
                {
                    if (!quotationDto.quotationDetails.Any(d =>
                        d.description == detail.Description &&
                        d.quantity == detail.QuotationQuantity &&
                        d.NetAmount == detail.QuotationAmount))
                    {
                        detailsChanged = true;
                        break;
                    }
                }

                // Check added or modified items
                if (!detailsChanged)
                {
                    foreach (var dtoDetail in quotationDto.quotationDetails)
                    {
                        var detail = existingDetails.FirstOrDefault(d => d.Description == dtoDetail.description);
                        if (detail == null)
                        {
                            detailsChanged = true; // new item added
                            break;
                        }
                        else if (
                            detail.ItemName != dtoDetail.itemName ||
                            detail.Description != dtoDetail.description ||
                            detail.QuotationQuantity != dtoDetail.quantity ||
                            detail.UnitRate != dtoDetail.price ||
                            detail.QuotationAmount != dtoDetail.NetAmount ||
                            detail.VAT != dtoDetail.tax)
                        {
                            detailsChanged = true; // existing item modified
                            break;
                        }
                    }
                }

                // ✅ If nothing changed, exit early
                if (!parentChanged && !detailsChanged)
                {
                    return Ok(new { message = "No changes found." });
                }

                // ===== Perform update if changed =====
                quotation.GUID = Guid.NewGuid();
                quotation.CompanyId = quotationDto.CompanyId;
                quotation.ReceivingEntity = quotationDto.receivingEntity;
                quotation.Currency = quotationDto.currency;
                quotation.PaymentTerms = quotationDto.PaymentTerms;
                quotation.SubTotal = quotationDto.subtotal ?? 0;
                quotation.TaxAmount = quotationDto.taxAmount ?? 0;
                quotation.TotalAmount = quotationDto.totalAmount ?? 0;
                quotation.QuotationType = quotationDto.quotationType;
                quotation.ModifiedDate = DateTime.Now;
                quotation.ModifiedBy = loggedUserId.ToString();
                quotation.CompanyName = quotationDto.CompanyName;
                quotation.CompanyEmail = quotationDto.CompanyEmail;
                quotation.CompanyPhoneNumber = quotationDto.CompanyPhoneNumber;
                quotation.CompanyAddress = quotationDto.CompanyAddress;
                quotation.VatNumber = quotationDto.VatNumber;
                quotation.RegistrationNumber = quotationDto.RegistrationNumber;
                quotation.CompanyWebsite = quotationDto.CompanyWebsite;
                quotation.BankAccountNumber = quotationDto.BankAccountNumber;
                quotation.BranchCode = quotationDto.BranchCode;
                quotation.BrannchAddress = quotationDto.BrannchAddress;
                quotation.IFSCCode = quotationDto.IFSCCode;
                quotation.CustomerName = quotationDto.CustomerName;
                quotation.CustomerEmail = quotationDto.CustomerEmail;
                quotation.CustomerPhone = quotationDto.CustomerPhone;
                quotation.CustomerAddress = quotationDto.CustomerAddress;
                quotation.Status = "Updated";
                quotation.AccountHolderName = quotationDto.AccountHolderName;
                quotation.BankName = quotationDto.BankName;
                quotation.DueDate = quotationDto.dueDate;
                _context.BillingQuotations.Update(quotation);
                await _auditService.AddQuotationAudit(quotation, "Quotation Updated Successfully");

                // ===== Update child details =====
                foreach (var detail in existingDetails)
                {
                    if (!quotationDto.quotationDetails.Any(d =>
                        d.description == detail.Description && d.amount == detail.QuotationAmount))
                    {
                        _context.BillingQuotationDetails.Remove(detail);
                    }
                }

                foreach (var dtoDetail in quotationDto.quotationDetails)
                {
                    var detail = existingDetails.FirstOrDefault(d => d.Description == dtoDetail.description);
                    if (detail == null)
                    {
                        var newDetail = new BillingQuotationDetails
                        {
                            CompanyId = quotation.CompanyId,
                            ReferenceNumber = quotation.ReferenceNumber,
                            ItemName = dtoDetail.itemName,
                            Description = dtoDetail.description,
                            QuotationQuantity = dtoDetail.quantity ?? 0m,
                            UnitRate = dtoDetail.price ?? 0m,
                            QuotationAmount = (int)dtoDetail.NetAmount,
                            VAT = dtoDetail.tax,
                            Discount = dtoDetail.Discount,
                            DiscountType = dtoDetail.DiscountType,
                            Category = dtoDetail.category,
                            CreatedDate = DateTime.Now,
                            CreatedBy = loggedUserId,
                            ModifiedDate = DateTime.Now,
                            ModifiedBy = loggedUserId,
                            ItemCode = dtoDetail.ItemCode,
                            InventoryId = dtoDetail.InventoryId,
                            DiscountValue = dtoDetail.DiscountValue

                        };
                        _context.BillingQuotationDetails.Add(newDetail);
                    }
                    else
                    {
                        detail.ItemName = dtoDetail.itemName;
                        detail.Description = dtoDetail.description;
                        detail.QuotationQuantity = dtoDetail.quantity ?? 0m;
                        detail.UnitRate = dtoDetail.price ?? 0m;
                        detail.VAT = dtoDetail.tax;
                        detail.Discount = dtoDetail.Discount;
                        detail.DiscountType = dtoDetail.DiscountType;
                        detail.Category = dtoDetail.category;
                        detail.ModifiedDate = DateTime.Now;
                        detail.ModifiedBy = loggedUserId;
                        detail.CompanyId = quotation.CompanyId; ;
                        detail.ItemCode = dtoDetail.ItemCode;
                        detail.QuotationAmount = (int)dtoDetail.NetAmount;
                        detail.InventoryId = dtoDetail.InventoryId;
                        detail.DiscountValue = dtoDetail.DiscountValue;

                        _context.BillingQuotationDetails.Update(detail);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // ===== Send Email =====
                var customeractivationUrl = $"{quotationDto.QuotationLink}?quotationId={quotation.ReferenceNumber}&guid={quotation.GUID}&type=customer";
                var companyactivationUrl = $"{quotationDto.QuotationLink}?quotationId={quotation.ReferenceNumber}&type=company";

                var CustomeremailMessage = new EmailMessage
                {
                    ToEmail = quotationDto.CustomerEmail,
                    Subject = "Quotation Sent Action-Required",
                    Body = $@"
                    <p>Dear {quotationDto.CustomerName},</p>
                    <p>Attached is your quotation for review.</p>
                    <p>To proceed, please click the link to view your Quotation and select either Accept or Decline based on your decision:
                    <a href='{customeractivationUrl}'>here</a>.</p>
                    <br/>
                     <p>Kind regards,<br/>ITSA Support Team</p>",
                    IsBodyHtml = true
                };

                await _emailService.SendEmailAsync(CustomeremailMessage);

                //var CompanyemailMessage = new EmailMessage
                //{
                //    ToEmail = quotationDto.CompanyEmail,
                //    Subject = "Quotation Sent Action-Required",
                //    Body = $@"
                //    <p>Dear {quotationDto.CompanyName},</p>
                //    <p>Attached is your quotation for review.</p>
                //    <p>To proceed, please click the link to view your Quotation and select either Accept or Decline based on your decision:
                //    <a href='{companyactivationUrl}'>here</a>.</p>
                //    <br/>
                //     <p>Kind regards,<br/>ITSA Support Team</p>",
                //    IsBodyHtml = true
                //};
                //await _emailService.SendEmailAsync(CompanyemailMessage);

                return Ok(new { message = "Quotation updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error updating quotation", error = ex.Message });
            }
        }

        [HttpGet("getInvoiceRefno")]
        public async Task<IActionResult> GetInvoiceRefNumber()
        {
            try
            {
                // 👉 Get last inserted quotation (IMPORTANT: use Id, not string sort)
                var lastInvoice = await _context.QuotationInvoice
                    .Where(x => x.InvoiceReferenceNumber.StartsWith("INV-"))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();


                int nextNumber = 1;

                if (lastInvoice != null && !string.IsNullOrEmpty(lastInvoice.InvoiceReferenceNumber))
                {
                    var parts = lastInvoice.InvoiceReferenceNumber.Split('-');

                    // Expected format: QUO-YYYY-MMDD-0001
                    if (parts.Length == 4 && int.TryParse(parts[3], out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }

                // 👉 Date part (only for display, not for sequence)
                string datePart = DateTime.Now.ToString("yyyy-MMdd");

                // 👉 Final reference number
                string newRefNo = $"INV-{datePart}-{nextNumber:D4}";

                return Ok(new
                {
                    success = true,
                    referenceNumber = newRefNo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generating reference number",
                    error = ex.Message
                });
            }
        }

        [HttpPost("saveInvoice")]
        public async Task<IActionResult> SaveInvoice([FromForm] string invoiceData)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            if (string.IsNullOrWhiteSpace(invoiceData))
                return BadRequest("Missing invoice data.");

            InvoiceDto invoiceDto;

            try
            {
                invoiceDto = JsonSerializer.Deserialize<InvoiceDto>(invoiceData);
            }
            catch
            {
                return BadRequest("Invalid JSON format.");
            }
            Console.WriteLine(invoiceData);

            Console.WriteLine(invoiceDto == null);

            Console.WriteLine(invoiceDto?.invoiceDetails == null);

            Console.WriteLine(invoiceDto?.invoiceDetails?.Count);
            if (invoiceDto == null || invoiceDto.invoiceDetails == null || !invoiceDto.invoiceDetails.Any())
                return BadRequest("Invoice details are empty.");

            try
            {
                string invoiceDetailsJson = JsonSerializer.Serialize(invoiceDto.invoiceDetails);
                if (invoiceDto.invoiceDate < System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                {
                    throw new Exception("InvoiceDate is invalid");
                }

                if (invoiceDto.DueDate < System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                {
                    throw new Exception("DueDate is invalid");
                }


                using SqlConnection con = new SqlConnection(_context.Database.GetConnectionString());

                using SqlCommand cmd = new SqlCommand("sp_SaveInvoice", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@InvoiceReferenceNumber", invoiceDto.invoiceId);
                cmd.Parameters.AddWithValue("@InvoiceDate", invoiceDto.invoiceDate);
                cmd.Parameters.AddWithValue("@ReceivingEntity", (object?)invoiceDto.receivingEntity ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@QuotationType", (object?)invoiceDto.quotationType ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@CompanyName", (object?)invoiceDto.CompanyName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@VatNumber", (object?)invoiceDto.VatNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RegistrationNumber", (object?)invoiceDto.RegistrationNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CompanyAddress", invoiceDto.CompanyAddress);
                cmd.Parameters.AddWithValue("@CompanyWebsite", (object?)invoiceDto.CompanyWebsite ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CompanyEmail", (object?)invoiceDto.CompanyEmail ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CompanyPhoneNumber", (object?)invoiceDto.CompanyPhoneNumber ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@CustomerName", (object?)invoiceDto.CustomerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CustomerAddress", (object?)invoiceDto.CustomerAddress ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CustomerEmail", (object?)invoiceDto.CustomerEmail ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CustomerPhone", (object?)invoiceDto.CustomerPhone ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@BankAccountNumber", (object?)invoiceDto.BankAccountNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BrannchAddress", (object?)invoiceDto.BrannchAddress ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BranchCode", (object?)invoiceDto.BranchCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IFSCCode", invoiceDto.IFSCCode);

                cmd.Parameters.AddWithValue("@SubTotal", (object?)invoiceDto.subtotal ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TaxAmount", (object?)invoiceDto.taxAmount ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TotalAmount", (object?)invoiceDto.totalAmount ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@PaymentTerms", (object?)invoiceDto.PaymentTerms ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CustomerRefNo", (object?)invoiceDto.customerRefNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AccountHolderName", (object?)invoiceDto.AccountHolderName ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@CompanyId", invoiceDto.CompanyId);
                cmd.Parameters.AddWithValue("@BankName", (object?)invoiceDto.BankName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DueDate", invoiceDto.DueDate);

                cmd.Parameters.AddWithValue("@FileName", (object?)invoiceDto.CompanyLogo ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@CreatedBy", loggedUserId.ToString());
                cmd.Parameters.AddWithValue("@ModifiedBy", loggedUserId.ToString());

                cmd.Parameters.AddWithValue("@InvoiceDetails", invoiceDetailsJson);

                await con.OpenAsync();

                int invoiceId = 0;
                string message = "";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var success = Convert.ToInt32(reader["Success"]);

                        if (success == 0)
                        {
                            return BadRequest(new
                            {
                                message = reader["Message"].ToString()
                            });
                        }

                        invoiceId = Convert.ToInt32(reader["InvoiceId"]);
                        message = reader["Message"].ToString();
                    }
                }

                var customerUrl = $"{invoiceDto.QuotationLink}?invoiceId={invoiceId}&type=customer";

                var customerEmail = new EmailMessage
                {
                    ToEmail = invoiceDto.CustomerEmail,
                    Subject = "Invoice Generation Notification",
                    Body = $@"
                <p>Dear {invoiceDto.CustomerName},</p>
            <p>Good Day,</p>
                <p>
                    To proceed please click
                    <a href='{customerUrl}'>here</a>.
                </p>
                <br/>
                <p>Kind regards,<br/>ITSA Support Team</p>",
                    IsBodyHtml = true


                };

                await _emailService.SendEmailAsync(customerEmail);


                return Ok(new
                {
                    success = true,
                    invoiceId,
                    message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message

                });
            }
        }

        [HttpPost("updateInvoice")]
        public async Task<IActionResult> UpdateInvoice([FromBody] InvoiceDto dto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            if (dto == null)
                return BadRequest("Invalid request.");

            if (dto.invoiceDetails == null || !dto.invoiceDetails.Any())
                return BadRequest("Invoice details are empty.");
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var invoice = await _context.QuotationInvoice
                    .FirstOrDefaultAsync(x => x.InvoiceReferenceNumber == dto.InvoiceReferenceNumber);

                if (invoice == null)
                    return NotFound("Invoice not found.");

                bool parentChanged =
                    invoice.CompanyId != dto.CompanyId ||
                    invoice.CompanyName != dto.CompanyName ||
                    invoice.VatNumber != dto.VatNumber ||
                    invoice.RegistrationNumber != dto.RegistrationNumber ||
                    invoice.CompanyAddress != dto.CompanyAddress ||
                    invoice.CompanyWebsite != dto.CompanyWebsite ||
                    invoice.CompanyEmail != dto.CompanyEmail ||
                    invoice.CompanyPhoneNumber != dto.CompanyPhoneNumber ||
                    invoice.BankAccountNumber != dto.BankAccountNumber ||
                    invoice.BranchCode != dto.BranchCode ||
                    invoice.BranchAddress != dto.BrannchAddress ||
                    invoice.IFSCCode != dto.IFSCCode ||
                    invoice.AccountHolderName != dto.AccountHolderName ||
                    invoice.BankName != dto.BankName ||
                    invoice.CustomerName != dto.CustomerName ||
                    invoice.CustomerPhone != dto.CustomerPhone ||
                    invoice.CustomerEmail != dto.CustomerEmail ||
                    invoice.CustomerAddress != dto.CustomerAddress ||
                    invoice.ReceivingEntity != dto.receivingEntity ||
                    invoice.SubTotal != (dto.subtotal ?? 0) ||
                    invoice.TaxAmount != (dto.taxAmount ?? 0) ||
                    invoice.TotalAmount != (dto.totalAmount ?? 0) ||
                    invoice.QuotationType != dto.quotationType ||
                    invoice.PaymentTerms != dto.PaymentTerms;

                var existingDetails = await _context.QuotationInvoiceDetails
                    .Where(d => d.ReferenceNumber == invoice.InvoiceReferenceNumber)
                    .ToListAsync();

                bool detailsChanged = false;

                if (existingDetails.Count != (dto.invoiceDetails?.Count ?? 0))
                {
                    detailsChanged = true;
                }
                else
                {
                    for (int i = 0; i < existingDetails.Count; i++)
                    {
                        var dbRow = existingDetails[i];
                        var dtoRow = dto.invoiceDetails[i];

                        if (dbRow.ItemName != dtoRow.itemName ||
                            dbRow.Description != dtoRow.description ||
                            dbRow.QuotationQuantity != dtoRow.quantity ||
                            dbRow.UnitRate != dtoRow.price ||
                            dbRow.QuotationAmount != dtoRow.NetAmount ||
                            dbRow.VAT != dtoRow.tax ||
                            dbRow.Discount != dtoRow.Discount ||
                            dbRow.DiscountType != dtoRow.DiscountType ||
                            dbRow.Category != dtoRow.category ||
                            dbRow.DiscountValue != dtoRow.DiscountValue)
                        {
                            detailsChanged = true;
                            break;
                        }
                    }
                }

                if (!parentChanged && !detailsChanged)
                {
                    return Ok(new { message = "No changes found." });
                }

                invoice.CompanyId = dto.CompanyId ?? invoice.CompanyId;
                invoice.CompanyName = dto.CompanyName;
                invoice.VatNumber = dto.VatNumber;
                invoice.RegistrationNumber = dto.RegistrationNumber;
                invoice.CompanyAddress = dto.CompanyAddress;
                invoice.CompanyWebsite = dto.CompanyWebsite;
                invoice.CompanyEmail = dto.CompanyEmail;
                invoice.CompanyPhoneNumber = dto.CompanyPhoneNumber;
                invoice.BankAccountNumber = dto.BankAccountNumber;
                invoice.BranchCode = dto.BranchCode;
                invoice.BranchAddress = dto.BrannchAddress;
                invoice.IFSCCode = dto.IFSCCode;
                invoice.AccountHolderName = dto.AccountHolderName;
                invoice.BankName = dto.BankName;
                invoice.CustomerName = dto.CustomerName;
                invoice.CustomerPhone = dto.CustomerPhone;
                invoice.CustomerEmail = dto.CustomerEmail;
                invoice.CustomerAddress = dto.CustomerAddress;
                invoice.ReceivingEntity = dto.receivingEntity;
                invoice.InvoiceDate = dto.invoiceDate;
                invoice.QuotationType = dto.quotationType;
                invoice.PaymentTerms = dto.PaymentTerms;
                invoice.SubTotal = dto.subtotal ?? 0;
                invoice.TaxAmount = dto.taxAmount ?? 0;
                invoice.TotalAmount = dto.totalAmount ?? 0;
                invoice.Status = "Updated";
                invoice.ModifiedDate = DateTime.Now;
                invoice.ModifiedBy = loggedUserId.ToString();
                invoice.CreatedBy = loggedUserId.ToString();
                invoice.CreatedDate = DateTime.Now;
                _context.QuotationInvoice.Update(invoice);

                _context.QuotationInvoiceDetails.RemoveRange(existingDetails);

                foreach (var item in dto.invoiceDetails ?? new List<InvoiceDetailDto>())
                {
                    _context.QuotationInvoiceDetails.Add(new QuotationInvoiceDetails
                    {
                        ReferenceNumber = invoice.InvoiceReferenceNumber,
                        ItemName = item.itemName,
                        Description = item.description,
                        QuotationQuantity = item.quantity,
                        UnitRate = item.price,
                        VAT = item.tax,
                        QuotationAmount = (int)item.NetAmount,
                        Discount = item.Discount,
                        DiscountType = item.DiscountType,
                        Category = item.category,
                        DiscountValue = item.DiscountValue,
                        ItemCode = item.ItemCode,
                        InventoryId = item.InventoryId,
                        CreatedDate = DateTime.Now,
                        CreatedBy = loggedUserId.ToString(),
                        ModifiedDate = DateTime.Now,
                        ModifiedBy = loggedUserId.ToString(),
                    });
                }
                await _auditService.AddInvoiceAudit(invoice, "Invoice updated Successfully");


                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Invoice updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error updating invoice", error = ex.Message });
            }
        }

        [HttpPost("updateProformaold")]
        public async Task<IActionResult> UpdateProformaold([FromBody] UpdateProformaDto model)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            try
            {
                if (model == null)
                    return BadRequest("Invalid request.");

                // Generate Proforma Reference Number only when status is Proforma Invoice Generated
                if (model.Status.Equals("Proforma Invoice Generated", StringComparison.OrdinalIgnoreCase))
                {
                    // Get the last generated Proforma Reference Number
                    var lastInvoice = await _context.BillingQuotations
                        .Where(x => x.PorformaReferenceNumber != null &&
                                    x.PorformaReferenceNumber.StartsWith("PI-"))
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefaultAsync();

                    int nextNumber = 1;

                    if (lastInvoice != null &&
                        !string.IsNullOrWhiteSpace(lastInvoice.PorformaReferenceNumber))
                    {
                        var parts = lastInvoice.PorformaReferenceNumber.Split('-');

                        // Expected format: PI-2026-0730-0001
                        if (parts.Length == 4 &&
                            int.TryParse(parts[3], out int lastNumber))
                        {
                            nextNumber = lastNumber + 1;
                        }
                    }

                    string datePart = DateTime.Now.ToString("yyyy-MMdd");

                    model.ProformaReferenceNumber = $"PI-{datePart}-{nextNumber:D4}";
                }

                await _context.Database.ExecuteSqlRawAsync(
 @"EXEC sp_CreateProforma
    @InvoiceReferenceNumber=@InvoiceReferenceNumber,
    @PorformaReferenceNumber=@PorformaReferenceNumber,
    @Status=@Status,
    @Reason=@Reason,
    @ModifiedBy=@ModifiedBy,
    @ActionBy=@ActionBy,
    @ActionDate=@ActionDate,
    @Action=@Action",
     new SqlParameter("@InvoiceReferenceNumber", model.InvoiceReferenceNumber),
     new SqlParameter("@PorformaReferenceNumber", (object?)model.ProformaReferenceNumber ?? DBNull.Value),
     new SqlParameter("@Status", model.Status),
     new SqlParameter("@Reason", (object?)model.Reason ?? DBNull.Value),
     new SqlParameter("@ModifiedBy", loggedUserId),
     new SqlParameter("@ActionBy", loggedUserId),
     new SqlParameter("@ActionDate", DateTime.Now),
     new SqlParameter("@Action", model.Status)
 );

                var invoice = await _context.BillingQuotations
                    .FirstOrDefaultAsync(x => x.ReferenceNumber == model.InvoiceReferenceNumber);

                if (invoice != null &&
        !string.IsNullOrWhiteSpace(invoice.CustomerEmail))
                {


                    var customerUrl = $"{model.TemplateUrl}?quotationId={model.InvoiceReferenceNumber}&type=customer";



                    var proformaText = string.Empty;

                    if (model.Status.Equals(
                            "Proforma Invoice Generated",
                            StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(model.ProformaReferenceNumber))
                    {
                        proformaText = $" <strong>{model.ProformaReferenceNumber}</strong>";
                    }

                    var emailBody = $@"
    <p>Dear {invoice.CustomerName},</p>
    <p>Good Day,</p>

    <p>
        Your quotation 
        <strong>{model.InvoiceReferenceNumber}</strong>
        has been updated to 
        <strong>{model.Status}</strong>{proformaText}.
    </p>";


                    //            var emailBody = $@"
                    //<p>Dear {invoice.CustomerName},</p>
                    //<p>Good Day,</p>

                    //<p>
                    //    Your quotation 
                    //    <strong>{model.InvoiceReferenceNumber}</strong>
                    //    has been updated to 
                    //    <strong>{model.Status}</strong>
                    //    {(!string.IsNullOrEmpty(model.ProformaReferenceNumber)
                    //                        ? $"<strong>{model.ProformaReferenceNumber}</strong>"
                    //                        : "")}.
                    //</p>";


                    if (model.Status.Equals(
                        "Proforma Invoice Generated",
                        StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(customerUrl))
                    {
                        emailBody += $@"
            <p>
                You can view your Proforma Invoice by clicking the link below:
            </p>

            <p>
                <a href=""{customerUrl}"">Click here</a>
            </p>";
                    }

                    emailBody += @"
        <br/>

        <p>
            Kind regards,<br/>
            ITSA Support Team
        </p>";

                    var customerEmail = new EmailMessage
                    {
                        ToEmail = invoice.CustomerEmail,
                        Subject = $"Quotation {model.Status}",
                        Body = emailBody,
                        IsBodyHtml = true
                    };

                    await _emailService.SendEmailAsync(customerEmail);
                }

                string referenceNumber = model.Status.Equals("Proforma Invoice Generated", StringComparison.OrdinalIgnoreCase) ? model.ProformaReferenceNumber : model.InvoiceReferenceNumber;

                return Ok(new
                {
                    success = true,
                    message = $"{referenceNumber} has been {model.Status} successfully",
                    referenceNumber = referenceNumber
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("getProformaRefno")]
        public async Task<IActionResult> GetProformaRefNumber()
        {
            try
            {
                // 👉 Get last inserted quotation (IMPORTANT: use Id, not string sort)
                var lastInvoice = await _context.QuotationInvoice
                    .Where(x => x.PorformaReferenceNumber.StartsWith("PI-"))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();


                int nextNumber = 1;

                if (lastInvoice != null && !string.IsNullOrEmpty(lastInvoice.PorformaReferenceNumber))
                {
                    var parts = lastInvoice.PorformaReferenceNumber.Split('-');

                    // Expected format: QUO-YYYY-MMDD-0001
                    if (parts.Length == 4 && int.TryParse(parts[3], out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }

                // 👉 Date part (only for display, not for sequence)
                string datePart = DateTime.Now.ToString("yyyy-MMdd");

                // 👉 Final reference number
                string newRefNo = $"PI-{datePart}-{nextNumber:D4}";

                return Ok(new
                {
                    success = true,
                    referenceNumber = newRefNo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generating reference number",
                    error = ex.Message
                });
            }
        }
        [HttpPost("UploadPaymentFiles")]
        public async Task<IActionResult> UploadPaymentFiles(
            [FromForm] string referenceNumber,
            [FromForm] string type,
            [FromForm] List<IFormFile> files)

        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }
            try
            {
                if (files == null || files.Count == 0)
                {
                    return BadRequest("No files uploaded.");
                }

                // Folder path
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "PaymentUploads");

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // Generate unique file name
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

                        var filePath = Path.Combine(uploadFolder, uniqueFileName);

                        // Save file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Save record
                        var paymentUpload = new PaymentUploads
                        {
                            ReferenceNumber = referenceNumber,
                            Type = type,
                            Filename = file.FileName,
                            Filepath = $"/PaymentUploads/{uniqueFileName}",
                            CreatedBy = loggedUserId.ToString(),
                            CreatedDate = DateTime.Now,
                            ModifiedBy = loggedUserId.ToString(),
                            ModifiedDate = DateTime.Now,

                        };

                        _context.PaymentUploads.Add(paymentUpload);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Files uploaded successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }



        [HttpGet("GetPaymentFiles/{referenceNumber}")]
        public async Task<IActionResult> GetPaymentFiles(string referenceNumber)
        {
            var files = await _context.PaymentUploads
                .Where(x => x.ReferenceNumber == referenceNumber)
                .Select(x => new
                {
                    x.Id,
                    x.ReferenceNumber,
                    x.Filename,
                    x.Filepath
                })
                .ToListAsync();

            if (!files.Any())
            {
                return NotFound("No files found.");
            }

            return Ok(files);
        }

        [HttpPost("record-payment")]
        public async Task<IActionResult> RecordPayment(
        [FromBody] RecordQuotationPaymentRequest request)
        {
            if (request.PaymentAmount <= 0)
                return BadRequest("Payment amount must be greater than zero.");

            var refno = _context.BillingQuotations.Where(x => x.Id == request.QuotationRefNo).Select(x => x.ReferenceNumber).FirstOrDefault();

            var previousPaid = await _context.QuotationPayments
                .Where(x => x.QuotationRefNo == refno)
                .SumAsync(x => (decimal?)x.PaidAmount) ?? 0;

            var installmentNumber = await _context.QuotationPayments
                .Where(x => x.QuotationRefNo == refno)
                .CountAsync() + 1;
            var transactionId = $"{refno}-{installmentNumber}";

            var totalAmount = request.TotalAmount;
            var newPaidAmount = previousPaid + request.PaymentAmount;

            if (newPaidAmount > totalAmount)
                return BadRequest("Payment cannot exceed the quotation total.");

            var status = newPaidAmount >= totalAmount
                ? "Paid"
                : request.DepositAmount > 0 &&
                  newPaidAmount >= request.DepositAmount
                    ? "Deposit Paid"
                    : "Partial Paid";


            var payment = new QuotationPayments
            {
                QuotationRefNo = refno,
                TotalAmount = totalAmount,
                TotatlDue = Math.Max(totalAmount - newPaidAmount, 0),
                PaidAmount = request.PaymentAmount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = request.PaymentMethod,
                TransactionId = transactionId,
            };

            _context.QuotationPayments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                payment.Id,
                payment.TransactionId,
                payment.PaidAmount,
                TotalPaid = newPaidAmount,
                payment.TotatlDue,
            });
        }

        [HttpGet("get-payments/{quotationRefNo}")]
        public async Task<IActionResult> GetQuotationPayments(string quotationRefNo)
        {


            var payments = await _context.QuotationPayments
                .Where(x => x.QuotationRefNo.ToString() == quotationRefNo)
                .OrderBy(x => x.PaymentDate)
                .Select(x => new
                {
                    x.Id,
                    x.QuotationRefNo,
                    x.TotalAmount,
                    x.TotatlDue,
                    x.PaidAmount,
                    x.PaymentDate,
                    x.PaymentMethod,
                    x.TransactionId
                })
                .ToListAsync();

            return Ok(payments);
        }

        [HttpPost("deleteQuotation/{id}")]
        public async Task<IActionResult> DeleteQuotation(int id)
        {
            try
            {
                // Get logged-in user's role
                var role = User.Claims
                    .FirstOrDefault(c =>
                        c.Type == "role" ||
                        c.Type == ClaimTypes.Role)
                    ?.Value;

                if (string.IsNullOrEmpty(role))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Unauthorized user."
                    });
                }

                // Only Super Admin can delete
                if (!role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase) &&
                    !role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Only Super Admin can delete quotations."
                    });
                }

                // Get logged-in user ID
                var userIdValue = User.Claims
                    .FirstOrDefault(c => c.Type == "userId")
                    ?.Value;

                if (!int.TryParse(userIdValue, out int userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User ID not found."
                    });
                }

                // Get quotation
                var quotation = await _context.BillingQuotations
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (quotation == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Quotation not found."
                    });
                }

                // Only Created, Updated and Approved quotations can be deleted
                var allowedStatuses = new[]
                {
    "Created",
    "Updated",
    "Approved"
};

                if (!allowedStatuses.Contains(quotation.Status))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Quotation with status '{quotation.Status}' cannot be deleted."
                    });
                }

                // Change status to Deleted
                quotation.Status = "Deleted";

                quotation.DeletedBy = userId.ToString();
                quotation.DeletedDate = DateTime.UtcNow;

                quotation.ModifiedDate = DateTime.UtcNow;
                quotation.ModifiedBy = userId.ToString();

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    RefNo = quotation.ReferenceNumber,
                    message = "Quotation deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("updateProforma")]

        public async Task<IActionResult> UpdateProforma([FromBody] UpdateProformaDto model)
        {
            var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            int loggedUserId = 0;

            if (!string.IsNullOrEmpty(userIdClaim) &&
                int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            try
            {
                if (model == null)
                    return BadRequest("Invalid request.");

                // Generate Proforma Reference Number only for Proforma Invoice Generated
                // Generate Proforma Reference Number ONLY for Proforma Invoice Generated
                if (model.Status.Equals(
                        "Proforma Invoice Generated",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var proformaReferences = await _context.BillingQuotations
                        .Where(x =>
                            x.PorformaReferenceNumber != null &&
                            x.PorformaReferenceNumber.StartsWith("PI-"))
                        .Select(x => x.PorformaReferenceNumber)
                        .ToListAsync();

                    int lastNumber = 0;

                    foreach (var reference in proformaReferences)
                    {
                        var parts = reference.Split('-');

                        if (parts.Length == 4 &&
                            int.TryParse(parts[3], out int number))
                        {
                            if (number > lastNumber)
                            {
                                lastNumber = number;
                            }
                        }
                    }

                    int nextNumber = lastNumber + 1;

                    string datePart = DateTime.Now.ToString("yyyy-MMdd");

                    model.ProformaReferenceNumber =
                        $"PI-{datePart}-{nextNumber:D4}";
                }
                else
                {
                    // No Proforma Reference Number for other statuses
                    model.ProformaReferenceNumber = null;
                }
                // Update database
                await _context.Database.ExecuteSqlRawAsync(
                    @"EXEC sp_CreateProforma 
                @InvoiceReferenceNumber=@InvoiceReferenceNumber, 
                @PorformaReferenceNumber=@PorformaReferenceNumber, 
                @Status=@Status, 
                @Reason=@Reason, 
                @ModifiedBy=@ModifiedBy, 
                @ActionBy=@ActionBy, 
                @ActionDate=@ActionDate, 
                @Action=@Action",
                    new SqlParameter(
                        "@InvoiceReferenceNumber",
                        model.InvoiceReferenceNumber),
                    new SqlParameter(
                        "@PorformaReferenceNumber",
                        (object?)model.ProformaReferenceNumber ?? DBNull.Value),
                    new SqlParameter(
                        "@Status",
                        model.Status),
                    new SqlParameter(
                        "@Reason",
                        (object?)model.Reason ?? DBNull.Value),
                    new SqlParameter(
                        "@ModifiedBy",
                        loggedUserId),
                    new SqlParameter(
                        "@ActionBy",
                        loggedUserId),
                    new SqlParameter(
                        "@ActionDate",
                        DateTime.Now),
                    new SqlParameter(
                        "@Action",
                        model.Status)
                );

                // Get quotation
                var invoice = await _context.BillingQuotations
                    .FirstOrDefaultAsync(x =>
                        x.ReferenceNumber == model.InvoiceReferenceNumber);

                // Send email based on status
                if (invoice != null &&
                    !string.IsNullOrWhiteSpace(invoice.CustomerEmail))
                {
                    await SendProformaStatusEmailAsync(invoice, model);
                }
                if (model.UnpaidStatus == "Unpaid" && model.Status == "Paid")
                {
                    var existsinvoice = await _context.QuotationInvoice
                        .FirstOrDefaultAsync(x =>
                            x.QuotationReferenceNumber == model.InvoiceReferenceNumber);

                    if (existsinvoice == null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Invoice not found."
                        });
                    }

                    existsinvoice.Status = "Created";
                    existsinvoice.ModifiedBy = loggedUserId.ToString();

                    _context.QuotationInvoice.Update(existsinvoice);

                    // IMPORTANT: await this
                    await _context.SaveChangesAsync();

                    await _auditService.AddInvoiceAudit(
                        existsinvoice,
                        "Unpaid Invoice is fully paid"
                    );
                }

                await _auditService.AddQuotationAudit(invoice, model.Status);


                string referenceNumber =
                    model.Status.Equals(
                        "Proforma Invoice Generated",
                        StringComparison.OrdinalIgnoreCase)
                        ? model.ProformaReferenceNumber
                        : model.InvoiceReferenceNumber;

                return Ok(new
                {
                    success = true,
                    message = $"{referenceNumber} has been {model.Status} successfully",
                    referenceNumber = referenceNumber
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        //  private async Task SendProformaStatusEmailAsyncold(
        //BillingQuotation invoice,
        //UpdateProformaDto model)
        //  {
        //      // Get ALL refund proof documents
        //      EmailMessage customerEmail;

        //      string status = model.Status?.Trim().ToLower() ?? "";

        //      string customerUrl =
        //          $"{model.TemplateUrl}" +
        //          $"?quotationId={model.InvoiceReferenceNumber}" +
        //          $"&type=customer";

        //      string emailBody;

        //      switch (status)
        //      {
        //          // =========================================================
        //          // PROFORMA INVOICE GENERATED
        //          // =========================================================
        //          case "proforma invoice generated":

        //              emailBody = $@"
        //          <p>Dear {invoice.CustomerName},</p>

        //          <p>Good Day,</p>

        //          <p>
        //              Your quotation
        //              <strong>{model.InvoiceReferenceNumber}</strong>
        //              has been updated to
        //              <strong>Proforma Invoice Generated</strong>.
        //          </p>

        //          <p>
        //              Proforma Invoice Reference:
        //              <strong>{model.ProformaReferenceNumber}</strong>
        //          </p>

        //          <p>
        //              You can view your Proforma Invoice by clicking the link below:
        //          </p>

        //          <p>
        //              <a href=""{customerUrl}"">Click here</a>
        //          </p>

        //          <br/>

        //          <p>
        //              Kind regards,<br/>
        //              ITSA Support Team
        //          </p>";

        //              break;



        //          // =========================================================
        //          // DEPOSIT PAID
        //          // =========================================================
        //          case "deposit paid":

        //              customerEmail = await BuildPaymentEmailAsync(
        //                  invoice,
        //                  model,
        //                  "Deposit Paid",
        //                  "We have received a deposit payment for your quotation."
        //              );

        //              break;


        //          // =========================================================
        //          // PARTIAL PAID
        //          // =========================================================
        //          case "partial paid":

        //              customerEmail = await BuildPaymentEmailAsync(
        //                  invoice,
        //                  model,
        //                  "Partial Payment Received",
        //                  "We have received a partial payment for your quotation."
        //              );

        //              break;


        //          // =========================================================
        //          // PAID
        //          // =========================================================
        //          case "paid":

        //              customerEmail = await BuildPaymentEmailAsync(
        //                  invoice,
        //                  model,
        //                  "Payment Received",
        //                  "Full payment has been received for your quotation."
        //              );

        //              break;


        //          // =========================================================
        //          // OVERDUE
        //          // =========================================================
        //          case "overdue":

        //              emailBody = $@"
        //          <p>Dear {invoice.CustomerName},</p>

        //          <p>Good Day,</p>

        //          <p>
        //              Your quotation
        //              <strong>{model.InvoiceReferenceNumber}</strong>
        //              is now
        //              <strong>Overdue</strong>.
        //          </p>

        //          <p>
        //              Please review the quotation and complete the required
        //              payment process.
        //          </p>

        //          <br/>

        //          <p>
        //              Kind regards,<br/>
        //              ITSA Support Team
        //          </p>";

        //              break;


        //          // =========================================================
        //          // REJECTED
        //          // =========================================================
        //          case "rejected":

        //              emailBody = $@"
        //          <p>Dear {invoice.CustomerName},</p>

        //          <p>Good Day,</p>

        //          <p>
        //              Your quotation
        //              <strong>{model.InvoiceReferenceNumber}</strong>
        //              has been
        //              <strong>Rejected</strong>.
        //          </p>

        //          {(string.IsNullOrWhiteSpace(model.Reason)
        //                      ? ""
        //                      : $@"
        //              <p>
        //                  <strong>Reason:</strong>
        //                  {model.Reason}
        //              </p>")}

        //          <br/>

        //          <p>
        //              Kind regards,<br/>
        //              ITSA Support Team
        //          </p>";

        //              break;


        //          // =========================================================
        //          // DEFAULT
        //          // =========================================================
        //          default:

        //              emailBody = $@"
        //          <p>Dear {invoice.CustomerName},</p>

        //          <p>Good Day,</p>

        //          <p>
        //              Your quotation
        //              <strong>{model.InvoiceReferenceNumber}</strong>
        //              has been updated to
        //              <strong>{model.Status}</strong>.
        //          </p>

        //          <br/>

        //          <p>
        //              Kind regards,<br/>
        //              ITSA Support Team
        //          </p>";

        //              break;
        //      }

        //      var customerEmail = new EmailMessage
        //      {
        //          ToEmail = invoice.CustomerEmail,
        //          Subject = $"Quotation {model.Status}",
        //          Body = emailBody,
        //          IsBodyHtml = true
        //      };

        //      await _emailService.SendEmailAsync(customerEmail);
        //  }


        //private async Task<string> BuildPaymentEmailAsync(
        //BillingQuotation invoice,
        //UpdateProformaDto model,
        //string paymentStatus,
        //string paymentMessage)
        //{
        //    var proofOfPayments = _files.GetProofOfPayments(invoice.ReferenceNumber, "Deposit Paid");

        //    // Get latest payment
        //    var payment = await _context.QuotationPayments
        //        .Where(x =>
        //            x.QuotationRefNo.ToString() == model.InvoiceReferenceNumber)
        //        .OrderByDescending(x => x.PaymentDate)
        //        .FirstOrDefaultAsync();

        //    if (payment == null)
        //    {
        //        return $@"
        //    <p>Dear {invoice.CustomerName},</p>

        //    <p>Good Day,</p>

        //    <p>
        //        {paymentMessage}
        //        <strong>{model.InvoiceReferenceNumber}</strong>.
        //    </p>

        //    <br/>

        //    <p>
        //        Kind regards,<br/>
        //        ITSA Support Team
        //    </p>";
        //    }

        //    string amountDueText = payment.TotatlDue > 0
        //        ? $@"
        //    <tr>
        //        <td style=""padding: 8px; border: 1px solid #ddd;"">
        //            Amount Due
        //        </td>
        //        <td style=""padding: 8px; border: 1px solid #ddd;"">
        //            <strong>{payment.TotatlDue:N2}</strong>
        //        </td>
        //    </tr>"
        //        : "";

        //    string remainingMessage = payment.TotatlDue > 0
        //        ? $@"
        //    <p>
        //        The remaining balance of
        //        <strong>{payment.TotatlDue:N2}</strong>
        //        is due for payment.
        //    </p>"
        //        : @"
        //    <p>
        //        Thank you for completing your payment.
        //    </p>";

        //    return $@"
        //<p>Dear {invoice.CustomerName},</p>

        //<p>Good Day,</p>

        //<p>
        //    {paymentMessage}
        //    <strong>{model.InvoiceReferenceNumber}</strong>.
        //</p>

        //<p>
        //    <strong>Payment Details:</strong>
        //</p>

        //<table style=""border-collapse: collapse; width: 100%; max-width: 600px;"">

        //    <tr>
        //        <td style=""padding: 8px; border: 1px solid #ddd;"">
        //            Payment Status
        //        </td>
        //        <td style=""padding: 8px; border: 1px solid #ddd;"">
        //            <strong>{paymentStatus}</strong>
        //        </td>
        //    </tr>

        //    <tr>
        //        <td style=""padding: 8px; border: 1px solid #ddd;"">
        //            Total Amount
        //        </td>
        //        <td style=""padding: 8px; border: 1px solid #ddd;"">
        //            <strong>{payment.TotalAmount:N2}</strong>
        //        </td>
        //    </tr>

        //    <tr>
        //        <td style=""padding: 8px; border: 1px solid #ddd;"">
        //            Amount Paid
        //        </td>
        //        <td style=""padding: 8px; border: 1px solid #ddd;"">
        //            <strong>{payment.PaidAmount:N2}</strong>
        //        </td>
        //    </tr>

        //    {amountDueText}

        //    <tr>
        //        <td style=""padding: 8px; border: 1px solid #ddd;"">
        //            Payment Date
        //        </td>
        //        <td style=""padding: 8px; border: 1px solid #ddd;"">
        //            <strong>{payment.PaymentDate:dd/MM/yyyy}</strong>
        //        </td>
        //    </tr>

        //    <tr>
        //        <td style=""padding: 8px; border: 1px solid #ddd;"">
        //            Payment Method
        //        </td>
        //        <td style=""padding: 8px; border: 1px solid #ddd;"">
        //            <strong>{payment.PaymentMethod}</strong>
        //        </td>
        //    </tr>

        //    {(string.IsNullOrWhiteSpace(payment.TransactionId)
        //                ? ""
        //                : $@"
        //        <tr>
        //            <td style=""padding: 8px; border: 1px solid #ddd;"">
        //                Transaction ID
        //            </td>
        //            <td style=""padding: 8px; border: 1px solid #ddd;"">
        //                <strong>{payment.TransactionId}</strong>
        //            </td>
        //        </tr>")}

        //</table>

        //{remainingMessage}

        //<br/>

        //<p>
        //    Kind regards,<br/>
        //    ITSA Support Team
        //</p>";
        //}
        private void AddPaymentProofAttachments(
    EmailMessage email,
    List<PaymentUploads> proofOfPayments)
        {
            if (proofOfPayments == null ||
                proofOfPayments.Count == 0)
            {
                return;
            }


            foreach (var proof in proofOfPayments)
            {
                if (proof == null)
                    continue;

                if (string.IsNullOrWhiteSpace(proof.Filepath))
                    continue;


                // ---------------------------------------------------------
                // DB path:
                // /PaymentUploads/abc.jpg
                //
                // Convert to:
                // PaymentUploads/abc.jpg
                // ---------------------------------------------------------

                var filePath = proof.Filepath.TrimStart('/', '\\');


                // ---------------------------------------------------------
                // Physical path
                // ---------------------------------------------------------

                var physicalFilePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    filePath
                );


                Console.WriteLine(
                    $"DB Path: {proof.Filepath}"
                );

                Console.WriteLine(
                    $"Physical Path: {physicalFilePath}"
                );

                Console.WriteLine(
                    $"File Exists: {System.IO.File.Exists(physicalFilePath)}"
                );


                // ---------------------------------------------------------
                // Add attachment
                // ---------------------------------------------------------

                if (System.IO.File.Exists(physicalFilePath))
                {
                    email.Attachments.Add(
                        new EmailAttachment
                        {
                            Filename = proof.Filename,
                            Filepath = physicalFilePath
                        }
                    );

                    Console.WriteLine(
                        $"Attachment added: {proof.Filename}"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"Attachment file NOT FOUND: {physicalFilePath}"
                    );
                }
            }
        }
        private async Task SendProformaStatusEmailAsync(
    BillingQuotation invoice,
    UpdateProformaDto model)
        {
            EmailMessage customerEmail;

            string status = model.Status?.Trim().ToLower() ?? "";

            string customerUrl =
                $"{model.TemplateUrl}" +
                $"?quotationId={model.InvoiceReferenceNumber}" +
                $"&type=customer";


            switch (status)
            {
                // =========================================================
                // PROFORMA INVOICE GENERATED
                // =========================================================
                case "proforma invoice generated":

                    customerEmail = new EmailMessage
                    {
                        ToEmail = invoice.CustomerEmail,

                        Subject = $"Quotation {model.Status}",

                        Body = $@"
                <p>Dear {invoice.CustomerName},</p>

                <p>Good Day,</p>

                <p>
                    Your quotation
                    <strong>{model.InvoiceReferenceNumber}</strong>
                    has been updated to
                    <strong>Proforma Invoice Generated</strong>.
                </p>

                <p>
                    Proforma Invoice Reference:
                    <strong>{model.ProformaReferenceNumber}</strong>
                </p>

                <p>
                    You can view your Proforma Invoice by clicking the link below:
                </p>

                <p>
                    <a href=""{customerUrl}"">Click here</a>
                </p>

                <br/>

                <p>
                    Kind regards,<br/>
                    ITSA Support Team
                </p>",

                        IsBodyHtml = true
                    };

                    break;


                // =========================================================
                // DEPOSIT PAID
                // =========================================================
                case "deposit paid":

                    customerEmail = await BuildPaymentEmailAsync(
                        invoice,
                        model,
                        "Deposit Paid",
                        "We have received a deposit payment for your quotation."
                    );

                    break;


                // =========================================================
                // PARTIAL PAID
                // =========================================================
                case "partial paid":

                    customerEmail = await BuildPaymentEmailAsync(
                        invoice,
                        model,
                        "Partial Payment Received",
                        "We have received a partial payment for your quotation."
                    );

                    break;


                // =========================================================
                // PAID
                // =========================================================
                case "paid":

                    customerEmail = await BuildPaymentEmailAsync(
                        invoice,
                        model,
                        "Payment Received",
                        "Full payment has been received for your quotation."
                    );

                    break;


                // =========================================================
                // OVERDUE
                // =========================================================
                case "overdue":

                    customerEmail = new EmailMessage
                    {
                        ToEmail = invoice.CustomerEmail,

                        Subject = $"Quotation {model.Status}",

                        Body = $@"
                <p>Dear {invoice.CustomerName},</p>

                <p>Good Day,</p>

                <p>
                    Your quotation
                    <strong>{model.InvoiceReferenceNumber}</strong>
                    is now
                    <strong>Overdue</strong>.
                </p>

                <p>
                    Please review the quotation and complete the required
                    payment process.
                </p>

                <br/>

                <p>
                    Kind regards,<br/>
                    ITSA Support Team
                </p>",

                        IsBodyHtml = true
                    };

                    break;


                // =========================================================
                // REJECTED
                // =========================================================
                case "rejected":

                    customerEmail = new EmailMessage
                    {
                        ToEmail = invoice.CustomerEmail,

                        Subject = $"Quotation {model.Status}",

                        Body = $@"
                <p>Dear {invoice.CustomerName},</p>

                <p>Good Day,</p>

                <p>
                    Your quotation
                    <strong>{model.InvoiceReferenceNumber}</strong>
                    has been
                    <strong>Rejected</strong>.
                </p>

                {(string.IsNullOrWhiteSpace(model.Reason)
                            ? ""
                            : $@"
                    <p>
                        <strong>Reason:</strong>
                        {model.Reason}
                    </p>")}

                <br/>

                <p>
                    Kind regards,<br/>
                    ITSA Support Team
                </p>",

                        IsBodyHtml = true
                    };

                    break;


                // =========================================================
                // DEFAULT
                // =========================================================
                default:

                    customerEmail = new EmailMessage
                    {
                        ToEmail = invoice.CustomerEmail,

                        Subject = $"Quotation {model.Status}",

                        Body = $@"
                <p>Dear {invoice.CustomerName},</p>

                <p>Good Day,</p>

                <p>
                    Your quotation
                    <strong>{model.InvoiceReferenceNumber}</strong>
                    has been updated to
                    <strong>{model.Status}</strong>.
                </p>

                <br/>

                <p>
                    Kind regards,<br/>
                    ITSA Support Team
                </p>",

                        IsBodyHtml = true
                    };

                    break;
            }


            // =========================================================
            // SEND EMAIL
            // =========================================================

            await _emailService.SendEmailAsync(customerEmail);
        }
        private async Task<List<PaymentUploads>> GetProofOfPayments(string referenceNumber, string type)
        {
            return await _context.PaymentUploads
                .Where(x =>
                    x.ReferenceNumber == referenceNumber &&
                    x.Type == type)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }
        private async Task<EmailMessage> BuildPaymentEmailAsync(
       BillingQuotation invoice,
       UpdateProformaDto model,
       string paymentStatus,
       string paymentMessage)
        {
            // =========================================================
            // DETERMINE PAYMENT PROOF TYPE
            // =========================================================

            string proofType = paymentStatus switch
            {
                "Deposit Paid" => "Deposit Paid",
                "Partial Payment Received" => "Partial Paid",
                "Payment Received" => "Paid",
                _ => null
            };


            // =========================================================
            // GET PAYMENT PROOF DOCUMENTS
            // =========================================================

            List<PaymentUploads> proofOfPayments = new();

            if (!string.IsNullOrWhiteSpace(proofType))
            {
                proofOfPayments = await GetProofOfPayments(
                    invoice.ReferenceNumber,
                    proofType
                );
            }


            Console.WriteLine(
                $"Payment Status: {paymentStatus}"
            );

            Console.WriteLine(
                $"Proof Type: {proofType}"
            );

            Console.WriteLine(
                $"Proof of Payments Count: {proofOfPayments.Count}"
            );


            // =========================================================
            // DEBUG PAYMENT PROOF FILES
            // =========================================================

            foreach (var proof in proofOfPayments)
            {
                Console.WriteLine(
                    $"Proof File Name: {proof?.Filename}"
                );

                Console.WriteLine(
                    $"Proof File Path: {proof?.Filepath}"
                );

                Console.WriteLine("----------------------------------------");
            }


            // =========================================================
            // GET LATEST PAYMENT
            // =========================================================

            var payment = await _context.QuotationPayments
                .Where(x =>
                    x.QuotationRefNo.ToString() ==
                    model.InvoiceReferenceNumber)
                .OrderByDescending(x => x.PaymentDate)
                .FirstOrDefaultAsync();


            // =========================================================
            // CREATE EMAIL
            // =========================================================

            var email = new EmailMessage
            {
                ToEmail = invoice.CustomerEmail,

                Subject = $"Payment Update - {model.InvoiceReferenceNumber}",

                IsBodyHtml = true,

                Attachments = new List<EmailAttachment>()
            };


            // =========================================================
            // PAYMENT RECORD NOT FOUND
            // =========================================================

            if (payment == null)
            {
                email.Body = $@"
        <div style='font-family:Arial, sans-serif; color:#000;'>

            <p>Dear {invoice.CustomerName},</p>

            <p>Good Day,</p>

            <p>
                {paymentMessage}
                <strong>{model.InvoiceReferenceNumber}</strong>.
            </p>

            <br/>

            <p>
                Kind regards,<br/>
                ITSA Support Team
            </p>

        </div>";

                // Attach proofs even if payment record is not found
                AddPaymentProofAttachments(
                    email,
                    proofOfPayments
                );

                return email;
            }


            // =========================================================
            // AMOUNT DUE
            // =========================================================

            string amountDueText = payment.TotatlDue > 0
                ? $@"
        <tr>
            <td style=""padding: 8px; border: 1px solid #ddd;"">
                Amount Due
            </td>

            <td style=""padding: 8px; border: 1px solid #ddd;"">
                <strong>{payment.TotatlDue:N2}</strong>
            </td>
        </tr>"
                : "";


            // =========================================================
            // REMAINING BALANCE MESSAGE
            // =========================================================

            string remainingMessage = payment.TotatlDue > 0
                ? $@"
        <p>
            The remaining balance of
            <strong>{payment.TotatlDue:N2}</strong>
            is due for payment.
        </p>"
                : @"
        <p>
            Thank you for completing your payment.
        </p>";


            // =========================================================
            // EMAIL BODY
            // =========================================================

            email.Body = $@"
    <div style='font-family:Arial, sans-serif; color:#000;'>

        <p>Dear {invoice.CustomerName},</p>

        <p>Good Day,</p>

        <p>
            {paymentMessage}
            <strong>{model.InvoiceReferenceNumber}</strong>.
        </p>

        <p>
            <strong>Payment Details:</strong>
        </p>

        <table style=""border-collapse: collapse; width: 100%; max-width: 600px;"">

    {(string.IsNullOrWhiteSpace(payment.TransactionId)
        ? ""
        : $@"
    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd;"">
            Transaction ID
        </td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">
            <strong>{payment.TransactionId}</strong>
        </td>
    </tr>")}

    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd;"">
            Total Amount
        </td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">
            <strong>{payment.TotalAmount:N2}</strong>
        </td>
    </tr>

    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd;"">
            Amount Paid
        </td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">
            <strong>{payment.PaidAmount:N2}</strong>
        </td>
    </tr>

    {amountDueText}

    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd;"">
            Paid Date
        </td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">
            <strong>{payment.PaymentDate:dd/MM/yyyy}</strong>
        </td>
    </tr>

    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd;"">
            Due Date
        </td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">
            <strong>{invoice.DueDate:dd/MM/yyyy}</strong>
        </td>
    </tr>

    <tr>
        <td style=""padding: 8px; border: 1px solid #ddd;"">
            Payment Method
        </td>
        <td style=""padding: 8px; border: 1px solid #ddd;"">
            <strong>{payment.PaymentMethod}</strong>
        </td>
    </tr>

    

</table>

        <br/>

        <p>
            Kind regards,<br/>
            ITSA Support Team
        </p>

    </div>";


            // =========================================================
            // ATTACH PAYMENT PROOF DOCUMENTS
            // =========================================================

            AddPaymentProofAttachments(
                email,
                proofOfPayments
            );


            // =========================================================
            // RETURN EMAIL
            // =========================================================

            return email;
        }
    }
}
        

