using InterCompanyBillingSulution.DataAcess.Data;
using InterCompanyBillingSulution.Models;
using InterCompanyBillingSulution.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;

namespace InterCompanyBillingSulution_Core.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]

    public class BillingController : ControllerBase
    {
        private readonly InterCompanyBillingDbContext _context;
        private readonly IEmailService _emailService;
        private readonly AuditService _auditService;
        public BillingController(InterCompanyBillingDbContext context, IEmailService emailService, AuditService auditService)
        {
            _context = context;
            _emailService = emailService;
            _auditService = auditService;
        }


        [HttpGet("GetQuotationsList")]
        public async Task<IActionResult> GetQuotationsList()
        {
            //await CheckQuotationAndInvoiceStatusAsync();
            var userIdClaim = User.Claims
                .FirstOrDefault(c => c.Type == "userId")?.Value;

            var loggedEmail = User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var loggedRole = User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            var companyIdClaim = User.Claims
                .FirstOrDefault(c => c.Type == "companyId")?.Value;

            // User ID
            int loggedUserId = 0;

            if (!string.IsNullOrEmpty(userIdClaim) &&
                int.TryParse(userIdClaim, out var userId))
            {
                loggedUserId = userId;
            }

            // Company ID
            int? companyId = null;

            if (!string.IsNullOrEmpty(companyIdClaim) &&
                int.TryParse(companyIdClaim, out var parsedCompanyId))
            {
                companyId = parsedCompanyId;
            }

            // Role check - case insensitive
            bool isSuperAdmin =
                string.Equals(loggedRole?.Trim(), "Super Admin",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(loggedRole?.Trim(), "SuperAdmin",
                    StringComparison.OrdinalIgnoreCase);
                

            List<QuotationDto> result = new();

            using var conn = _context.Database.GetDbConnection();

            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SP_GetApprovedQuotations";
            cmd.CommandType = CommandType.StoredProcedure;

            // CompanyId
            var companyParameter = new SqlParameter("@CompanyId", SqlDbType.Int)
            {
                Value = companyId.HasValue
                    ? companyId.Value
                    : DBNull.Value
            };

            cmd.Parameters.Add(companyParameter);

            // IsSuperAdmin
            cmd.Parameters.Add(
                new SqlParameter("@IsSuperAdmin", SqlDbType.Bit)
                {
                    Value = isSuperAdmin
                });

            // UserId
            cmd.Parameters.Add(
                new SqlParameter("@UserId", SqlDbType.Int)
                {
                    Value = loggedUserId
                });

            using var reader = await cmd.ExecuteReaderAsync();

            // ============================================================
            // HEADER
            // ============================================================

            while (await reader.ReadAsync())
            {
                result.Add(new QuotationDto
                {
                    QuotationId = reader["Id"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(reader["Id"]),

                    QuotationReferenceNumber =
                        reader["ReferenceNumber"]?.ToString(),

                    ReceivingEntity =
                        reader["ReceivingEntity"]?.ToString(),

                    QuotationDate =
                        reader["QuotationDate"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(reader["QuotationDate"]),
                   
                    
                    DueDate =
                        reader["DueDate"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(reader["DueDate"]),

                    QuotationStatus =
                        reader["Status"]?.ToString(),

                    Currency =
                        reader["Currency"]?.ToString(),

                    CompanyName =
                        reader["CompanyName"]?.ToString(),

                    VatNumber =
                        reader["VatNumber"]?.ToString(),

                    CompanyEmail =
                        reader["CompanyEmail"]?.ToString(),

                    CompanyAddress =
                        reader["CompanyAddress"]?.ToString(),

                    CompanyPhoneNumber =
                        reader["CompanyPhoneNumber"]?.ToString(),

                    BankAccountNumber =
                        reader["BankAccountNumber"]?.ToString(),

                    IFSCCode =
                        reader["IFSCCode"]?.ToString(),

                    BranchCode =
                        reader["BranchCode"]?.ToString(),

                    BranchAddress =
                        reader["BrannchAddress"]?.ToString(),

                    CustomerName =
                        reader["CustomerName"]?.ToString(),

                    CustomerEmail =
                        reader["CustomerEmail"]?.ToString(),

                    CustomerAddress =
                        reader["CustomerAddress"]?.ToString(),

                    CustomerPhone =
                        reader["CustomerPhone"]?.ToString(),

                    CompanyLogo =
                        reader["Filename"]?.ToString(),

                    RegistrationNumber =
                        reader["RegistrationNumber"]?.ToString(),

                    PaymentTerms =
                        reader["PaymentTerms"]?.ToString(),

                    TotalAmount =
                        reader["TotalAmount"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["TotalAmount"]),

                    SubTotal =
                        reader["SubTotal"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["SubTotal"]),

                    BankName =
                        reader["BankName"]?.ToString(),

                    AccountHolderName =
                        reader["AccountHolderName"]?.ToString(),

                    PorformaReferenceNumber =
                        reader["PorformaReferenceNumber"] == DBNull.Value
                            ? null
                            : reader["PorformaReferenceNumber"]?.ToString(),
                    UnpaidStatus =
                        reader["UnpaidStatus"] == DBNull.Value
                            ? null
                            : reader["UnpaidStatus"]?.ToString(),

                    PaidAmount =
                        reader["PaidAmount"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["PaidAmount"]),

                    OutstandingAmount =
                        reader["OutstandingAmount"] == DBNull.Value
                          ? 0
                       : Convert.ToDecimal(reader["OutstandingAmount"]),



                    Detail = new List<QuotationDetailDto>()
                });
            }

            // ============================================================
            // DETAILS
            // ============================================================

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    string referenceNumber =
                        reader["ReferenceNumber"]?.ToString();

                    var quotation = result.FirstOrDefault(x =>
                        x.QuotationReferenceNumber == referenceNumber);

                    if (quotation != null)
                    {
                        quotation.Detail.Add(new QuotationDetailDto
                        {
                            DetailId =
                                reader["DetailId"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(reader["DetailId"]),

                            QuotationAmount =
                                reader["QuotationAmount"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["QuotationAmount"]),

                            QuotationQuantity =
                                reader["QuotationQuantity"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["QuotationQuantity"]),

                            UnitRate =
                                reader["UnitRate"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["UnitRate"]),

                            InventoryId =
                                reader["InventoryId"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(reader["InventoryId"]),

                            Description =
                                reader["Description"]?.ToString(),

                            Category =
                                reader["Category"]?.ToString(),

                            ItemCode =
                                reader["ItemCode"]?.ToString(),

                            ItemName =
                                reader["ItemName"]?.ToString(),

                            Discount =
                                reader["Discount"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["Discount"]),

                            DiscountType =
                                reader["DiscountType"]?.ToString(),

                            Tax =
                                reader["VAT"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["VAT"])
                        });
                    }
                }
            }
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    string referenceNumber =
                        reader["QuotationRefNo"]?.ToString();

                    var quotation = result.FirstOrDefault(x =>
                        x.QuotationReferenceNumber == referenceNumber);

                    if (quotation != null)
                    {
                        quotation.PaymentHistory.Add(new QuotationPaymentDto
                        {
                            PaymentId =
                                reader["PaymentId"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(reader["PaymentId"]),

                            QuotationRefNo =
                                reader["QuotationRefNo"]?.ToString(),

                            TransactionId =
                                reader["TransactionId"] == DBNull.Value
                                    ? null
                                    : reader["TransactionId"]?.ToString(),

                            PaymentDate =
                                reader["PaymentDate"] == DBNull.Value
                                    ? DateTime.MinValue
                                    : Convert.ToDateTime(reader["PaymentDate"]),

                            PaidAmount =
                                reader["PaidAmount"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["PaidAmount"]),

                            

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

            return Ok(new
            {
                list = result
            });
            return Ok(new
            {
                list = result
            });
        }


        [HttpGet("getcompaniesList")]
        public async Task<IActionResult> GetCompaniesList()
        {

            var companieslist = await _context.Companies.ToListAsync();

            return Ok(new { list = companieslist });
        }

        [HttpGet("getqouationdetailswithrefno")]
        [AllowAnonymous]
        public async Task<IActionResult> GetQouationDetailsWithRefno(string refNo,string? guId, string? type)
        {
            // Expiry Validation
            var quotation = await _context.BillingQuotations
                .FirstOrDefaultAsync(x => x.ReferenceNumber == refNo);

            if (quotation == null)
                return BadRequest(new { message = "Quotation not found" });

            if (string.Equals(type, "customer", StringComparison.OrdinalIgnoreCase))
            {
                if (!Guid.TryParse(guId, out Guid parsedGuid))
                {
                    return BadRequest(new
                    {
                        message = "Invalid quotation link."
                    });
                }

                if (quotation.GUID != parsedGuid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid quotation link. The quotation has been updated to another customer."
                    });
                }
            }


            if ((quotation.Status == "Created" || quotation.Status == "Updated") &&
                quotation.QuotationDate < DateTime.UtcNow.AddDays(-14))
            {
                quotation.Status = "Expired";

                _context.BillingQuotations.Update(quotation);
                await _context.SaveChangesAsync();

                await _auditService.AddQuotationAuditwithoutsession(
                    quotation,
                    "Quotation is older than 14 days and has expired.");

                return BadRequest(new
                {
                    message = "Quotation is older than 14 days and has expired."
                });
            }

            if ((quotation.Status == "Created" || quotation.Status == "Updated") && quotation.DueDate < DateTime.UtcNow)
            {
                quotation.Status = "Overdue";

                _context.BillingQuotations.Update(quotation);
                await _context.SaveChangesAsync();

                await _auditService.AddQuotationAuditwithoutsession(
                    quotation,
                    "Quotation due date has passed and is overdue.");

                return BadRequest(new
                {
                    message = "Quotation due date has passed and is overdue."
                });
            }
            QuotationDto quotationDto = null;
            List<QuotationDetailDto> details = new();

            using (var conn = _context.Database.GetDbConnection())
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SP_GetQuotationDetailsByRefNo";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@RefNo", refNo));

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        // Header
                        if (await reader.ReadAsync())
                        {
                            quotationDto = new QuotationDto
                            {
                                Type = reader["Type"].ToString(),
                                QuotationId = Convert.ToInt32(reader["QuotationId"]),
                                QuotationReferenceNumber = reader["ReferenceNumber"].ToString(),
                                ReceivingEntity = reader["ReceivingEntity"].ToString(),
                                QuotationDate = Convert.ToDateTime(reader["QuotationDate"]),
                                DueDate = Convert.ToDateTime(reader["DueDate"]),
                                QuotationStatus = reader["QuotationStatus"].ToString(),
                                Currency = reader["Currency"].ToString(),
                                CompanyName = reader["CompanyName"].ToString(),
                                CompanyWebsite = reader["CompanyWebsite"].ToString(),
                                VatNumber = reader["VatNumber"].ToString(),
                                CompanyEmail = reader["CompanyEmail"].ToString(),
                                CompanyAddress = reader["CompanyAddress"].ToString(),
                                CompanyPhoneNumber = reader["CompanyPhoneNumber"].ToString(),
                                BankAccountNumber = reader["BankAccountNumber"].ToString(),
                                IFSCCode = reader["IFSCCode"].ToString(),
                                BranchCode = reader["BranchCode"].ToString(),
                                BranchAddress = reader["BranchAddress"].ToString(),
                                CustomerRefno = reader["CustomerRefno"].ToString(),
                                CustomerName = reader["CustomerName"].ToString(),
                                CustomerEmail = reader["CustomerEmail"].ToString(),
                                CustomerAddress = reader["CustomerAddress"].ToString(),
                                CustomerPhone = reader["CustomerPhone"].ToString(),
                                CompanyLogo = reader["CompanyLogo"].ToString(),
                                RegistrationNumber = reader["RegistrationNumber"].ToString(),
                                QuotationType = reader["QuotationType"].ToString(),
                                PaymentTerms = reader["PaymentTerms"].ToString(),
                                AccountHolderName = reader["AccountHolderName"].ToString(),
                                SubTotal = Convert.ToDecimal(reader["SubTotal"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                TaxAmount = Convert.ToDecimal(reader["TaxAmount"]),
                                BankName = reader["BankName"].ToString(),
                                Reason = reader["Reason"] == DBNull.Value ? null : reader["Reason"].ToString(),
                                PorformaReferenceNumber = reader["PorformaReferenceNumber"] == DBNull.Value? null: reader["PorformaReferenceNumber"].ToString(),
                                
                            };
                        }

                        // Details
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                details.Add(new QuotationDetailDto
                                {
                                    DetailId = Convert.ToInt32(reader["DetailId"]),
                                    QuotationAmount = Convert.ToDecimal(reader["QuotationAmount"]),
                                    QuotationQuantity = Convert.ToDecimal(reader["QuotationQuantity"]),
                                    UnitRate = Convert.ToDecimal(reader["UnitRate"]),
                                    Discount = Convert.ToDecimal(reader["Discount"]),
                                    DiscountType = reader["DiscountType"].ToString(),
                                    Tax = Convert.ToDecimal(reader["VAT"]),
                                    InventoryId = Convert.ToInt32(reader["InventoryId"]),
                                    Description = reader["Description"].ToString(),
                                    Category = reader["Category"].ToString(),
                                    ItemCode = reader["ItemCode"].ToString(),
                                    ItemName = reader["ItemName"].ToString()
                                });
                            }
                        }
                    }
                }
            }

            return Ok(new
            {
                quotation = quotationDto,
                details = details
            });
        }

        [HttpPost("saveInvoicegeneration")]
        public async Task<IActionResult> SaveInvoiceGeneration(string Refno, string templateurl,string UnpaidStatus ,string InvoiceStatus)
        {
            var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            using var connection = new SqlConnection(_context.Database.GetConnectionString());
            using var command = new SqlCommand("SP_SaveInvoiceGeneration", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@RefNo", Refno);
            command.Parameters.AddWithValue("@CreatedBy", loggedUserId);
            command.Parameters.AddWithValue("@QuotationStatus", InvoiceStatus);
            command.Parameters.AddWithValue("@UnpaidStatus", UnpaidStatus);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return NotFound(new { message = "Quotation not found or not approved." });
            }

            var invoiceId = Convert.ToInt32(reader["InvoiceId"]);
            var invoiceRefNo = reader["InvoiceReferenceNumber"].ToString();
            var quotationRefNo = reader["QuotationReferenceNumber"].ToString();
            var customerEmail = reader["CustomerEmail"].ToString();
            var customerName = reader["CustomerName"].ToString();
            var companyEmail = reader["CompanyEmail"].ToString();
            var companyName = reader["CompanyName"].ToString();

            await reader.CloseAsync();

            var invoice = await _context.QuotationInvoice
                .FirstOrDefaultAsync(x => x.Id == invoiceId);

            if (invoice != null && InvoiceStatus == "Unpaid")
            {
                await _auditService.AddInvoiceAudit(invoice, "Quotation Unpaid and generated to Invoice Successfully");
            }
            else if (invoice != null && InvoiceStatus == "Paid")
            {
                await _auditService.AddInvoiceAudit(invoice, "Quotation Paid and generated to Invoice Successfully");
            }

            var templateUrl = $"{templateurl}?invoiceId={invoiceId}";

            var emailMessage = new EmailMessage
            {
                ToEmail = customerEmail,
                Subject = "Invoice Generation Notification",
                Body = $@"
            <p>Dear {customerName},</p>
            <p>Good Day,</p>
            <p>Invoice <strong>{invoiceRefNo}</strong> has been generated successfully for quotation <strong>{quotationRefNo}</strong>.</p>
            <p>You can view your invoice by clicking the link below:</p>
            <p><a href='{templateUrl}'>here</a>.</p>

            <br/>
            <p>Kind Regards,<br/>ITSA</p>",
                IsBodyHtml = true
            };

            await _emailService.SendEmailAsync(emailMessage);

            // Optional: Send to company as well
            /*
            var companyMail = new EmailMessage
            {
                ToEmail = companyEmail,
                Subject = "Invoice Generation Notification",
                Body = $@"
                    <p>Dear {companyName},</p>
                    <p>Invoice <strong>{invoiceRefNo}</strong> has been generated successfully.</p>
                    <p><a href='{templateUrl}'>View Invoice</a></p>",
                IsBodyHtml = true
            };

            await _emailService.SendEmailAsync(companyMail);
            */

            return Ok(new
            {
                message = $"Invoice {invoiceRefNo} generated successfully for quotation {quotationRefNo}.",
                invoiceId
            });
        }

        //[HttpGet("getinvoicedetailswithrefno")]
        //[AllowAnonymous]
        //public async Task<IActionResult> getinvoicedetailswithrefno(int refNo)
        //{
        //    var list = await (from invoice in _context.QuotationInvoice

        //                      join detail in _context.QuotationInvoiceDetails
        //                          on invoice.InvoiceReferenceNumber equals detail.ReferenceNumber
        //                          into detailsGroup
        //                      where invoice.Id == refNo
        //                      select new
        //                      {
        //                          InvoiceId = invoice.Id,
        //                          QuotationReferenceNumber = invoice.QuotationReferenceNumber,
        //                          InvoiceReferenceNumber = invoice.InvoiceReferenceNumber,
        //                          QuotationType = invoice.QuotationType,
        //                          ReceivingEntity = invoice.ReceivingEntity,
        //                          QuotationStatus = invoice.Status,
        //                          Currency = invoice.Currency,
        //                          CompanyWebsite = invoice.CompanyWebsite,
        //                          CompanyName = invoice.CompanyName,
        //                          VatNumber = invoice.VatNumber,
        //                          CompanyEmail = invoice.CompanyEmail,
        //                          CompanyAddress = invoice.CompanyAddress,
        //                          CompanyPhoneNumber = invoice.CompanyPhoneNumber,
        //                          BankAccountNumber = invoice.BankAccountNumber,
        //                          IFSCCode = invoice.IFSCCode,
        //                          BranchCode = invoice.BranchCode,
        //                          BrannchAddress = invoice.BranchAddress,
        //                          CustomerName = invoice.CustomerName,
        //                          CustomerRefno = invoice.CustomerRefno,
        //                          CustomerEmail = invoice.CustomerEmail,
        //                          CustomerAddress = invoice.CustomerAddress,
        //                          CustomerPhone = invoice.CustomerPhone,
        //                          ComapanyLogo = invoice.Filename,
        //                          RegistrationNumber = invoice.RegistrationNumber,
        //                          PorformaReferenceNumber = invoice.PorformaReferenceNumber,
        //                          InvoiceDate = invoice.InvoiceDate,
        //                          PaymentTerms = invoice.PaymentTerms,
        //                          InvoiceStatus = invoice.Status,
        //                          Reason = invoice.RejectionReason,
        //                          AccountHolderName = invoice.AccountHolderName,
        //                          SubTotal = invoice.SubTotal,
        //                          TotalAmount = invoice.TotalAmount,
        //                          TaxAmount = invoice.TaxAmount,
        //                          BankName = invoice.BankName,

        //                          // Grouped details
        //                          Details = (from d in _context.QuotationInvoiceDetails
        //                                     join i in _context.InventoryItem on d.InventoryId equals i.Id.ToString()
        //                                     where d.ReferenceNumber == invoice.InvoiceReferenceNumber
        //                                     select new
        //                                     {
        //                                         d.ReferenceNumber,
        //                                         DetailId = d.Id,
        //                                         d.QuotationAmount,
        //                                         d.QuotationQuantity,
        //                                         d.UnitRate,
        //                                         InventoryId = i.Id,
        //                                         i.Description,
        //                                         i.Category,
        //                                         i.ItemCode,
        //                                         i.ItemName,
        //                                         d.Discount,
        //                                         d.DiscountType,
        //                                         d.VAT
        //                                     }).ToList()
        //                      }).ToListAsync();



        //    return Ok(new { list = list });
        //}

        [HttpGet("getinvoicedetailswithrefno")]
        [AllowAnonymous]
        public async Task<IActionResult> getinvoicedetailswithrefno(int refNo)
        {
            var list = await (
            from invoice in _context.QuotationInvoice

            join detail in _context.QuotationInvoiceDetails
                on invoice.InvoiceReferenceNumber equals detail.ReferenceNumber
                into detailsGroup

            from detail in detailsGroup.DefaultIfEmpty()

            join cmp in _context.Companies
     on invoice.CompanyId equals cmp.Id.ToString() into companyGroup

            from cmp in companyGroup.DefaultIfEmpty()
            where invoice.Id == refNo
            select new
                              {
                                  InvoiceId = invoice.Id,
                                  QuotationReferenceNumber = invoice.QuotationReferenceNumber,
                                  InvoiceReferenceNumber = invoice.InvoiceReferenceNumber,
                                  QuotationType = invoice.QuotationType,
                                  ReceivingEntity = invoice.ReceivingEntity,
                                  QuotationStatus = invoice.Status,
                                  Currency = invoice.Currency,
                                  CompanyWebsite = invoice.CompanyWebsite,
                                  CompanyName = invoice.CompanyName,
                                  VatNumber = invoice.VatNumber,
                                  CompanyEmail = invoice.CompanyEmail,
                                  CompanyAddress = invoice.CompanyAddress,
                                  CompanyPhoneNumber = invoice.CompanyPhoneNumber,
                                  BankAccountNumber = invoice.BankAccountNumber,
                                  IFSCCode = invoice.IFSCCode,
                                  BranchCode = invoice.BranchCode,
                                  BrannchAddress = invoice.BranchAddress,
                                  CustomerName = invoice.CustomerName,
                                  CustomerRefno = invoice.CustomerRefno,
                                  CustomerEmail = invoice.CustomerEmail,
                                  CustomerAddress = invoice.CustomerAddress,
                                  CustomerPhone = invoice.CustomerPhone,
                                  ComapanyLogo = cmp.CompanyLogo,
                                  RegistrationNumber = invoice.RegistrationNumber,
                                  PorformaReferenceNumber = invoice.PorformaReferenceNumber,
                                  InvoiceDate = invoice.InvoiceDate,
                                  PaymentTerms = invoice.PaymentTerms,
                                  InvoiceStatus = invoice.Status,
                                  Reason = invoice.RejectionReason,
                                  AccountHolderName = invoice.AccountHolderName,
                                  SubTotal = invoice.SubTotal,
                                  TotalAmount = invoice.TotalAmount,
                                  TaxAmount = invoice.TaxAmount,
                                  BankName = invoice.BankName,

                                  // Grouped details
                                  Details = (from d in _context.QuotationInvoiceDetails
                                             join i in _context.InventoryItem on d.InventoryId equals i.Id.ToString()
                                             where d.ReferenceNumber == invoice.InvoiceReferenceNumber
                                             select new
                                             {
                                                 d.ReferenceNumber,
                                                 DetailId = d.Id,
                                                 d.QuotationAmount,
                                                 d.QuotationQuantity,
                                                 d.UnitRate,
                                                 InventoryId = i.Id,
                                                 i.Description,
                                                 i.Category,
                                                 i.ItemCode,
                                                 i.ItemName,
                                                 d.Discount,
                                                 d.DiscountType,
                                                 d.VAT
                                             }).ToList()
                              }).ToListAsync();



            return Ok(new { list = list });
        }

        [HttpPost("savequotationApprovalorRejection")]
        [AllowAnonymous]
        public async Task<IActionResult> SaveQuotationApprovalorRejection([FromBody] QuotationActionRequestDTO request)
        {
            //var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            //int loggedUserId = 0;
            //if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            //{
            //    loggedUserId = id;
            //}

            var quotation = await _context.BillingQuotations
                .FirstOrDefaultAsync(x => x.ReferenceNumber == request.QuotationId);
            var quotationDetails = await _context.BillingQuotationDetails
            .Where(x => x.ReferenceNumber == quotation.ReferenceNumber)
            .ToListAsync();

            //var totalAmount = quotationDetails.Sum(x =>
            //{
            //    var baseAmount = (x.QuotationQuantity ?? 0) * (x.UnitRate ?? 0);
            //    var taxAmount = baseAmount * (x.Tax / 100m);
            //    return baseAmount + taxAmount;
            //});

            //totalAmount = Math.Round(totalAmount, 2);


            if (quotation == null)
            {
                return NotFound(new { message = "Quotation not found." });
            }
            
            var today = DateTime.UtcNow;


            // Expired after 14 days
            if ((quotation.Status == "Created" ||
                 quotation.Status == "Updated" ||
                 quotation.Status == "Approved" ||
                 quotation.Status == "Proforma Invoice Generated") &&
                quotation.QuotationDate < today.AddDays(-14))
            {
                quotation.Status = "Expired";
                quotation.ModifiedDate = today;

                _context.BillingQuotations.Update(quotation);
                await _context.SaveChangesAsync();

                await _auditService.AddQuotationAuditwithoutsession(
                    quotation,
                    "Quotation is older than 14 days and has expired."
                );

                return BadRequest(new
                {
                    message = "Quotation has expired and cannot be processed."
                });
            }

            // Overdue
            if ((quotation.Status == "Created" ||
                 quotation.Status == "Updated" ||
                 quotation.Status == "Approved") &&
                quotation.DueDate.Date < today.Date)
            {
                quotation.Status = "Overdue";
                quotation.ModifiedDate = today;

                _context.BillingQuotations.Update(quotation);
                await _context.SaveChangesAsync();

                await _auditService.AddQuotationAuditwithoutsession(
                    quotation,
                    "Quotation is overdue."
                );

                return BadRequest(new
                {
                    message = "Quotation is overdue and cannot be processed."
                });
            }
            // Get the customer ID using the quotation email
            var customerId = await _context.Customers
                .Where(x => x.Email == quotation.CustomerEmail)
                .Select(x => x.Email)
                .FirstOrDefaultAsync();

            if (customerId == null) // Assuming Id is int; adjust if nullable or GUID
            {
                return BadRequest(new { message = "Customer not found for the given quotation." });
            }


            if (request.Type == "Approve")
            {
                quotation.Status = "Approved";
                quotation.ModifiedBy = customerId.ToString();
                quotation.ModifiedDate = DateTime.Now;
            }
            else if (request.Type == "Reject")
            {
                quotation.Status = "Rejected";
                quotation.RejectionReason = request.Reason;
                quotation.RejectedDate = DateTime.Now;
                quotation.ModifiedBy = customerId.ToString();
                quotation.ModifiedDate = DateTime.Now;
            }

            _context.BillingQuotations.Update(quotation);
            await _context.SaveChangesAsync();
            if (request.Type == "Approve")
            {
                await _auditService.AddQuotationAuditwithoutsession(quotation, "Quotation Approved Successfully");

            }
            else
            {
                await _auditService.AddQuotationAuditwithoutsession(quotation, "Quotation Closed Successfully");

            }

            //    var emailMessage = new EmailMessage
            //    {
            //        ToEmail = quotation.CustomerEmail,
            //        Subject = $"Quotation Status Update - {quotation.ReferenceNumber}",
            //        Body = $@"
            //<div style='font-family:Arial, sans-serif; color:#000;'>
            //    <h2>Quotation Status Update - {quotation.ReferenceNumber}</h2>

            //    <p>Dear {quotation.CustomerEmail},</p>

            //    <p>We hope this message finds you well.</p>

            //    <p>This is to inform you that Quotation <strong>#{quotation.ReferenceNumber}</strong>, 
            //    dated {quotation.QuotationDate:dd/MM/yyyy}, has been <strong>{quotation.Status}</strong>.</p>

            //    <p><strong>Quotation Status:</strong> {quotation.Status}<br/>

            //    {(quotation.Status == "Rejected" ? $"<li><strong>Reason:</strong> {quotation.RejectionReason}</li>" : "")}
            //       <strong>Amount:{totalAmount}</strong> <br/>

            //    <br/>
            //    <p>Kind Regards,<br/>

            //    <strong>ITSA</strong><br/>
            //    {quotation.CompanyPhoneNumber}</p>
            //</div>",
            //        IsBodyHtml = true
            //    };
            //    await _emailService.SendEmailAsync(emailMessage);
            var companyemail = new EmailMessage
            {
                ToEmail = quotation.CompanyEmail,
                Subject = $"Quotation Status Update - {quotation.ReferenceNumber}",
                Body = $@"
                <div style='font-family:Arial, sans-serif; color:#000;'>
                  <h2>Quotation Status Update - {quotation.ReferenceNumber}</h2>
                  <p>Dear {quotation.CompanyName},</p>
                  <p>We hope this message finds you well.</p>
                  <p>This is to inform you that Quotation <strong>{quotation.ReferenceNumber}</strong>,dated {quotation.QuotationDate:dd/MM/yyyy}, has been <strong>{quotation.Status}</strong>.</p>
                  <p><strong>Quotation Status:</strong> {quotation.Status}
                  <br/>
                  <strong>Amount:{quotation.TotalAmount}</strong> <br/>
                  {(quotation.Status == "Rejected" ? $"<strong>Reason:</strong> {quotation.RejectionReason}" : "")}
                  <br/>
                  <p>Kind Regards,<br/>
                  <strong>ITSA</strong><br/>
                </div>",
                IsBodyHtml = true
            };
            await _emailService.SendEmailAsync(companyemail);

            return Ok(new { message = $"Quotation {quotation.ReferenceNumber} has been {request.Type.ToLower()} successfully." });
        }


        [HttpGet("getInvoicesList")]
        public async Task<IActionResult> GetInvoicesList()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            var loggedRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var companyId = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;

            bool isSuperAdmin = loggedRole?.Trim().ToLower() == "super admin" || loggedRole?.Trim().ToLower() == "superadmin";

            List<QutationInvioceDto> invoices = new();
            List<QuotationInvoiceDetailDto> details = new();
            List<QuotationPaymentDto> paymentHistory = new();
            using SqlConnection con = new SqlConnection(_context.Database.GetConnectionString());
            using SqlCommand cmd = new SqlCommand("SP_GetInvoicesList", con);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyId", (object?)companyId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsSuperAdmin", isSuperAdmin);
            cmd.Parameters.AddWithValue("@UserId", loggedUserId);

            await con.OpenAsync();

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            // Header
            while (await reader.ReadAsync())
            {
                invoices.Add(new QutationInvioceDto
                {
                    QuotationId = reader["QuotationId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["QuotationId"]),
                    QuotationDate = reader["QuotationDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["QuotationDate"]),
                    QuotationReferenceNumber = reader["QuotationReferenceNumber"] == DBNull.Value ? "" : reader["QuotationReferenceNumber"].ToString(),
                    QuotationStatus = reader["QuotationStatus"] == DBNull.Value ? "" : reader["QuotationStatus"].ToString(),
                    ReceivingEntity = reader["ReceivingEntity"].ToString(),
                    Currency = reader["Currency"].ToString(),
                    CompanyName = reader["CompanyName"].ToString(),
                    VatNumber = reader["VatNumber"].ToString(),
                    CompanyEmail = reader["CompanyEmail"].ToString(),
                    CompanyAddress = reader["CompanyAddress"].ToString(),
                    CompanyPhoneNumber = reader["CompanyPhoneNumber"].ToString(),
                    BankAccountNumber = reader["BankAccountNumber"].ToString(),
                    CompanyWebsite = reader["CompanyWebsite"].ToString(),
                    IFSCCode = reader["IFSCCode"].ToString(),
                    BranchCode = reader["BranchCode"].ToString(),
                    BranchAddress = reader["BranchAddress"].ToString(),
                    CustomerName = reader["CustomerName"].ToString(),
                    CustomerEmail = reader["CustomerEmail"].ToString(),
                    CustomerAddress = reader["CustomerAddress"].ToString(),
                    CustomerPhone = reader["CustomerPhone"].ToString(),
                    CompanyLogo = reader["CompanyLogo"].ToString(),
                    RegistrationNumber = reader["RegistrationNumber"].ToString(),
                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                    SubTotal = Convert.ToDecimal(reader["SubTotal"]),
                    TaxAmount = Convert.ToDecimal(reader["TaxAmount"]),
                    PaymentTerms = reader["PaymentTerms"].ToString(),
                    InvoiceReferenceNumber = reader["InvoiceReferenceNumber"].ToString(),
                    InvoiceStatus = reader["InvoiceStatus"].ToString(),
                    InvoiceId = Convert.ToInt32(reader["InvoiceId"]),
                    InvoiceDate = Convert.ToDateTime(reader["InvoiceDate"]),
                    Reason = reader["Reason"] == DBNull.Value ? null : reader["Reason"].ToString(),
                    PorformaReferenceNumber = reader["PorformaReferenceNumber"] == DBNull.Value ? null : reader["PorformaReferenceNumber"].ToString(),
                    QuotationType = reader["QuotationType"].ToString(),
                    BankName = reader["BankName"].ToString(),
                    AccountHolderName = reader["AccountHolderName"].ToString(),
                    Detail = new List<QuotationInvoiceDetailDto>()
                });
            }

            await reader.NextResultAsync();

            // Details
            while (await reader.ReadAsync())
            {
                details.Add(new QuotationInvoiceDetailDto
                {
                    ReferenceNumber = reader["ReferenceNumber"]?.ToString(),
                    DetailId = Convert.ToInt32(reader["DetailId"]),
                    QuotationAmount = Convert.ToDecimal(reader["QuotationAmount"]),
                    QuotationQuantity = Convert.ToDecimal(reader["QuotationQuantity"]),
                    UnitRate = Convert.ToDecimal(reader["UnitRate"]),
                    InventoryId = Convert.ToInt32(reader["InventoryId"]),
                    Description = reader["Description"].ToString(),
                    Category = reader["Category"].ToString(),
                    ItemCode = reader["ItemCode"].ToString(),
                    ItemName = reader["ItemName"].ToString(),
                    Discount = reader["Discount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Discount"]),
                    DiscountType = reader["DiscountType"].ToString(),
                    Tax = Convert.ToDecimal(reader["TaxAmount"])
                });
            }
            bool hasPayments = await reader.NextResultAsync();

            Console.WriteLine(
                $"Payments result set exists: {hasPayments}");
            if (hasPayments)
            {
                while (await reader.ReadAsync())
                {
                    paymentHistory.Add(new QuotationPaymentDto
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
                                ? DateTime.MinValue
                                : Convert.ToDateTime(reader["PaymentDate"]),

                        PaidAmount =
                            reader["PaidAmount"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader["PaidAmount"]),

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


            Console.WriteLine(
                $"Invoices fetched: {invoices.Count}");

            Console.WriteLine(
                $"Details fetched: {details.Count}");

            Console.WriteLine(
                $"Payments fetched: {paymentHistory.Count}");


            // =========================================================
            // ATTACH DETAILS TO EACH INVOICE
            // =========================================================

            foreach (var invoice in invoices)
            {
                invoice.Detail = details
                    .Where(x =>
                        x.ReferenceNumber ==
                        invoice.InvoiceReferenceNumber)
                    .ToList();
            }


            // =========================================================
            // ATTACH PAYMENT HISTORY TO EACH INVOICE
            // =========================================================

            foreach (var invoice in invoices)
            {
                invoice.PaymentHistory = paymentHistory
                    .Where(x =>
                        x.QuotationRefNo ==
                        invoice.QuotationReferenceNumber)
                    .ToList();
            }


            // =========================================================
            // RESPONSE
            // =========================================================

            return Ok(new
            {
                list = invoices
            });
        }

        [HttpPost("savequotationIvoiceApprovalPaidorCancel")]
        public async Task<IActionResult> savequotationIvoiceApprovalPaidorCancel(
    [FromBody] InvoiceActionRequestDTO request)
        {
            var loggedEmail = User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }


            var invoice = await _context.QuotationInvoice
                .FirstOrDefaultAsync(x => x.Id == request.InvoiceId);

            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found." });
            }

            // Update invoice status
            switch (request.Type)
            {
                case "Completed":
                    invoice.Status = "Order Completed";
                    break;

                case "Refund":
                    invoice.Status = "Refunded";
                    break;

                case "Cancelled":
                    invoice.Status = "Cancelled";
                    invoice.CancelledBy = request.cancelledBy;
                    invoice.RejectionReason = request.Reason;
                    invoice.RejectedDate = DateTime.Now;
                    break;

                default:
                    return BadRequest(new { message = "Invalid invoice action." });
            }

            invoice.ModifiedBy = loggedUserId.ToString();
            invoice.ModifiedDate = DateTime.Now;

            _context.QuotationInvoice.Update(invoice);
            await _context.SaveChangesAsync();

            // Audit
            var auditMessage = request.Type switch
            {
                "Completed" => "Invoice Order Completed Successfully",
                "Refund" => "Invoice Refunded Successfully",
                "Cancelled" => "Invoice Cancelled Successfully",
                _ => "Invoice Status Updated"
            };

            await _auditService.AddInvoiceAudit(invoice, auditMessage);

            // -----------------------------------------
            // CUSTOMER EMAIL
            // -----------------------------------------

            EmailMessage customerEmail;

            if (request.Type == "Refund")
            {
                customerEmail = await CreateRefundEmail(invoice);
            }
            else
            {
                //customerEmail = CreateInvoiceStatusEmail(invoice);
                customerEmail = CreateInvoiceStatusEmail(
                invoice,
                 request.TemplateUrl
                 );
            }

            if (!string.IsNullOrWhiteSpace(customerEmail.ToEmail))
            {
                await _emailService.SendEmailAsync(customerEmail);
            }

            // -----------------------------------------
            // COMPANY EMAIL
            // -----------------------------------------

            if (!string.IsNullOrWhiteSpace(invoice.CompanyEmail))
            {
                var companyEmail = CreateCompanyStatusEmail(invoice);

                await _emailService.SendEmailAsync(companyEmail);
            }

            return Ok(new
            {
                message =
                    $"Invoice {invoice.InvoiceReferenceNumber} order has been {request.Type.ToLower()} successfully."
            });
        }
        //[HttpGet("getCustomersList")]
        //public async Task<IActionResult> getCustomersList()
        //{
        //    var loggedRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        //    int? loggedCompanyId = null;
        //    var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;

        //    if (int.TryParse(companyIdClaim, out var parsedCompanyId))
        //    {
        //        loggedCompanyId = parsedCompanyId;
        //    }

        //    bool isSuperAdmin = loggedRole == "Super Admin" || loggedRole == "SuperAdmin";


        //    // Base query
        //    var customerQuery =
        //        from customer in _context.Customers
        //        join company in _context.Companies
        //            on customer.CompanyId equals company.Id
        //        select new
        //        {
        //            customer,
        //            company
        //        };

        //    // Apply company filter for non-super admins
        //    if (!isSuperAdmin)
        //    {
        //        if (loggedCompanyId == null)
        //        {
        //            return Unauthorized("CompanyId claim is missing or invalid.");
        //        }

        //        customerQuery = customerQuery
        //            .Where(x => x.company.Id == loggedCompanyId);
        //    }

        //    // Final projection
        //    var customersList = await customerQuery
        //        .Select(x => new
        //        {
        //            x.customer.Id,
        //            x.customer.UserName,
        //            x.customer.Name,
        //            x.customer.Gender,
        //            x.customer.SurName,
        //            x.customer.Email,
        //            x.customer.Mobile,
        //            x.customer.Address,
        //            x.customer.CustomerRefNo,
        //            CompanyName = x.company.CompanyName,
        //            x.customer.CompanyId,
        //            x.customer.Title,
        //            x.customer.DepartmentId,
        //            x.customer.IsActive,
        //            x.customer.IsDeleted
        //        })
        //        .ToListAsync();

        //    return Ok(new { list = customersList });
        //}

        //[HttpPost("savecustomerdetails")]
        //public async Task<IActionResult> savecustomerdetails([FromBody] CustomersDTO model)
        //{
        //    var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;


        //    var emailExists = _context.Customers.Any(x => x.Email == model.email);

        //    if (emailExists)
        //    {
        //        return NotFound(new { message = "Email already exists" });
        //    }

        //    var companyExists = _context.Customers.Any(x => x.CompanyId == model.companyId && x.Email == model.email);

        //    if (companyExists)
        //    {
        //        return NotFound(new { message = "Company already exists with this email" });
        //    }


        //    var lastCustomer = _context.Customers.OrderByDescending(c => c.Id).FirstOrDefault();

        //    string newRefNo = "REF_01";

        //    if (lastCustomer != null && !string.IsNullOrEmpty(lastCustomer.CustomerRefNo))
        //    {
        //        // Extract last number: REF_05 → 5
        //        var numberPart = lastCustomer.CustomerRefNo.Replace("REF_", "");
        //        if (int.TryParse(numberPart, out int lastNumber))
        //        {
        //            newRefNo = $"REF_{(lastNumber + 1).ToString("00")}";
        //        }
        //    }



        //    var Customer = new Customers
        //    {
        //        CustomerRefNo = newRefNo,
        //        Title = model.title,
        //        Name = model.name,
        //        Email = model.email,
        //        UserName = "NULL",
        //        SurName = model.surname,
        //        Mobile = model.mobile,
        //        Address = model.address,
        //        CompanyId = model.companyId,
        //        IsActive = "Yes",
        //        IsDeleted = "No",
        //        Activation = "Active",
        //        RoleName = "Customer",
        //        CreatedDate = DateTime.Now,
        //        ModifiedDate = DateTime.Now,
        //        CreatedBy = loggedEmail,
        //        ModifiedBy = loggedEmail,
        //        Gender = model.gender,

        //    };

        //    _context.Customers.Add(Customer);
        //    await _context.SaveChangesAsync();

        //    await _auditService.AddCustomerAuditAsync(Customer, "Customer Created Successfully");

        //    return Ok(new { message = $"Customer Reference No {newRefNo} saved successfully" });
        //}

        // [HttpPost("updatecustomerdetails")]
        // public async Task<IActionResult> updatecustomerdetails([FromBody] CustomersDTO model)
        // {
        //     var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        //     // Check if customer exists
        //     var customer = await _context.Customers.FirstOrDefaultAsync(x => x.Id == model.Id);
        //     if (customer == null)
        //     {
        //         return NotFound(new { message = "Customer not found" });
        //     }

        //     // Optional: check if email already exists for another customer
        //     var emailExists = await _context.Customers.AnyAsync(x => x.Email == model.email && x.Id != model.Id);
        //     if (emailExists)
        //     {
        //         return BadRequest(new { message = "Email already exists for another customer" });
        //     }
        //     bool hasChanges =
        //customer.Title != model.title ||
        //customer.Name != model.name ||
        //customer.Email != model.email ||
        //customer.SurName != model.surname ||
        //customer.Mobile != model.mobile ||
        //customer.IsActive != model.isActive ||
        //customer.IsDeleted != model.isDeleted ||
        //customer.Address != model.address ||
        //customer.CompanyId != model.companyId ||
        //customer.Gender != model.gender;

        //     if (!hasChanges)
        //     {
        //         return BadRequest(new { message = "No changes found." });
        //     }

        //     // Update fields
        //     customer.Title = model.title;
        //     customer.Name = model.name;
        //     customer.Email = model.email;
        //     customer.UserName = "Null";
        //     customer.SurName = model.surname;
        //     customer.Mobile = model.mobile;
        //     customer.IsActive = model.isActive;
        //     customer.IsDeleted = model.isDeleted;
        //     customer.Address = model.address;
        //     customer.CompanyId = model.companyId;
        //     customer.Gender = model.gender;
        //     customer.ModifiedDate = DateTime.Now;
        //     customer.ModifiedBy = loggedEmail;

        //     await _context.SaveChangesAsync();
        //     await _auditService.AddCustomerAuditAsync(customer, "Customer Updated Successfully");


        //     return Ok(new { message = "Customer details updated successfully" });
        // }

        //[HttpPost("deletecustomerdetails")]
        //public async Task<IActionResult> deletecustomerdetails(int Id)
        //{
        //    var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        //    // Check if customer exists
        //    var customer = await _context.Customers.FirstOrDefaultAsync(x => x.Id == Id);
        //    if (customer == null)
        //    {
        //        return NotFound(new { message = "Customer not found" });
        //    }

        //    customer.IsActive = "No";
        //    customer.IsDeleted = "Yes";
        //    customer.ModifiedDate = DateTime.Now;
        //    customer.ModifiedBy = loggedEmail;

        //    // Save changes
        //    await _context.SaveChangesAsync();
        //    await _auditService.AddCustomerAuditAsync(customer, "Customer delete status updated Successfully");

        //    return Ok(new { message = "Customer details deleted successfully" });
        //}


        [HttpGet("getQuotationorInvoicecount")]
        public async Task<IActionResult> GetQuotationOrInvoiceCount()
        {
            var loggedRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            var loggedCompanyId = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;


            bool isSuperAdmin = loggedRole == "Super Admin" || loggedRole == "SuperAdmin";
            IQueryable<BillingQuotation> quotationsQuery = _context.BillingQuotations;
            IQueryable<QuotationInvoice> invoiceQuery = _context.QuotationInvoice;

            invoiceQuery = from a in _context.QuotationInvoice
                           join b in _context.BillingQuotations on a.QuotationReferenceNumber equals b.ReferenceNumber

                           select a;

            if (!isSuperAdmin)
            {
                quotationsQuery = quotationsQuery.Where(x => x.CompanyId == loggedCompanyId);
                invoiceQuery = from a in _context.QuotationInvoice
                               join b in _context.BillingQuotations on a.QuotationReferenceNumber equals b.ReferenceNumber
                               where b.CompanyId == loggedCompanyId
                               select a;
            }
            // Quotation counts
            var quotationTotalCount = await quotationsQuery.CountAsync();
            var quotationCreatedCount = await quotationsQuery
                .Where(x => x.Status == "Created" || x.Status == "Updated")
                .CountAsync();
            var quotationApprovedCount = await quotationsQuery
                .Where(x => x.Status == "Approved")
                .CountAsync();
            var quotationRejectedCount = await quotationsQuery
                .Where(x => x.Status == "Rejected")
                .CountAsync();
            var quotationExpiredCount = await quotationsQuery
                .Where(x => x.Status == "Expired")
                .CountAsync();

            // Invoice counts
            var invoiceTotalCount = await invoiceQuery.CountAsync();
            var invoiceCreatedCount = await invoiceQuery
                .Where(x => x.Status == "Created")
                .CountAsync();
            var invoiceApprovedCount = await invoiceQuery
                .Where(x => x.Status == "Paid")
                .CountAsync();
            var invoiceRejectedCount = await invoiceQuery
                .Where(x => x.Status == "Cancelled")
                .CountAsync();
            var creditNoteCount = await invoiceQuery
               .Where(x => x.Status == "Credit note Created")
               .CountAsync();
            return Ok(new
            {
                Quotations = new
                {
                    Total = quotationTotalCount,
                    Created = quotationCreatedCount,
                    Approved = quotationApprovedCount,
                    Rejected = quotationRejectedCount,
                    Expired = quotationExpiredCount
                },
                Invoices = new
                {
                    Total = invoiceTotalCount,
                    Created = invoiceCreatedCount,
                    Approved = invoiceApprovedCount,
                    Rejected = invoiceRejectedCount,
                    CreditNote = creditNoteCount

                }
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

        [HttpPost("save-credit-note")]
        public async Task<IActionResult> SaveCreditNote([FromBody] CreditNoteDTO model)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            if (model == null || model.Header == null)
                return BadRequest("Invalid data");
            var invoice = await _context.QuotationInvoice.FirstOrDefaultAsync(x => x.InvoiceReferenceNumber == model.Header.InoviceReferenceNumber);

            try
            {
                var exists = await _context.CreditNoteHeader
               .AnyAsync(x => x.CreditNoteReferenceNumber == model.Header.CreditNoteReferenceNumber);

                if (exists)
                {
                    return Ok(new { success = false, message = "Credit Note Number already exists." });
                }


                var lastQuotation = await _context.CreditNoteDetails
        .Where(x => x.CreditNoteReferenceNumber.StartsWith("CN-"))
        .OrderByDescending(x => x.Id)
        .FirstOrDefaultAsync();

                int nextNumber = 1;

                if (lastQuotation != null && !string.IsNullOrEmpty(lastQuotation.CreditNoteReferenceNumber))
                {
                    var parts = lastQuotation.CreditNoteReferenceNumber.Split('-');

                    // Expected: CN-2026-0819-0001
                    if (parts.Length == 4 && int.TryParse(parts[3], out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }

                string datePart = DateTime.Now.ToString("yyyy-MMdd");

                string refNo = $"CN-{datePart}-{nextNumber:D4}";
                // 🔹 Map Header
                var header = new CreditNoteHeader
                {
                    CreditNoteReferenceNumber = refNo,
                    CreditNoteDate = model.Header.CreditNoteDate ?? DateTime.Now,
                    DueDate = model.Header.DueDate ?? DateTime.Now,
                    InoviceReferenceNumber = model.Header.InoviceReferenceNumber ?? "",

                    CompanyName = model.Header.CompanyName ?? "",
                    VatNumber = model.Header.VatNumber ?? "",
                    RegistrationNumber = model.Header.RegistrationNumber ?? "",
                    CompanyAddress = model.Header.CompanyAddress ?? "",
                    CompanyWebsite = model.Header.CompanyWebsite ?? "",
                    CompanyEmail = model.Header.CompanyEmail ?? "",
                    CompanyPhoneNumber = model.Header.CompanyPhoneNumber ?? "",

                    BankAccountNumber = model.Header.BankAccountNumber ?? "",
                    BrannchAddress = model.Header.BrannchAddress ?? "",
                    BranchCode = model.Header.BranchCode ?? "",
                    IFSCCode = model.Header.IFSCCode ?? "",
                    PaymentTerms = model.Header.PaymentTerms ?? "",
                    Currency = model.Header.Currency ?? "",
                    QuotationType = model.Header.QuotationType ?? "",
                    AccountHolderName = model.Header.AccountHolderName ?? "",

                    CustomerName = model.Header.CustomerName ?? "",
                    CustomerEmail = model.Header.CustomerEmail ?? "",
                    CustomerPhone = model.Header.CustomerPhone ?? "",
                    CustomerAddress = model.Header.CustomerAddress ?? "",
                    CustomerRefno = model.Header.CustomerRefno ?? "",

                    SubTotal = model.Header.SubTotal ?? 0,
                    TaxAmount = model.Header.TaxAmount ?? 0,
                    TotalAmount = model.Header.TotalAmount ?? 0,

                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    ModifiedBy = loggedUserId,
                    CreatedBy = loggedUserId,

                };

                _context.CreditNoteHeader.Add(header);
                await _context.SaveChangesAsync();

                // 🔹 Map Details
                foreach (var d in model.Details)
                {
                    var detail = new CreditNoteDetails
                    {
                        CreditNoteReferenceNumber =refNo ?? "",
                        InvoiceReferenceNumber = header.InoviceReferenceNumber ?? "",
                        ItemCode = d.ItemCode ?? "",
                        Category = d.Category ?? "",
                        ItemName = d.ItemName ?? "",
                        ItemDescription = d.ItemName ?? "",
                        DiscountType = d.DiscountType ?? "",
                        ItemQuantity = d.ItemQuantity ?? 0,
                        UnitRate = d.UnitRate ?? 0,
                        DiscountValue = d.DiscountValue,
                        Discount = d.Discount ?? 0,
                        VAT = d.VAT ?? 0,
                        ItemAmount = (int)(d.ItemAmount ?? 0),
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        ModifiedBy = loggedUserId.ToString(),
                        CreatedBy = loggedUserId.ToString(),
                    };

                    _context.CreditNoteDetails.Add(detail);
                }

                await _context.SaveChangesAsync();
                await _auditService.AddInvoiceAudit(invoice, "Credit Note " + refNo + " Created Successfully");

                //await transaction.CommitAsync();
                if (invoice != null)
                {
                    invoice.Status = "Credit note Created";
                    invoice.ModifiedBy = loggedUserId.ToString();
                    _context.QuotationInvoice.Update(invoice);
                    await _context.SaveChangesAsync();

                }

                var customerUrl = $"{model.Header.activationUrl}?creditNoteId={header.Id}";

                var creditNoteEmail = new EmailMessage
                {
                    ToEmail = header.CustomerEmail,
                    Subject = $"Credit Note - {header.CreditNoteReferenceNumber}",
                    Body = $@"
    <p>Dear {model.Header.CustomerName},</p>

    <p>We hope you are doing well.</p>

    <p>Your <strong>Credit Note</strong> 
    <strong>{header.CreditNoteReferenceNumber}</strong> has been generated successfully.</p>

    <p>You can view the details of your Credit Note by clicking the link below:</p>

    <p><a href='{customerUrl}'>View Credit Note</a></p>

    <br/>

    <p>If you have any questions or need further clarification, please feel free to contact us.</p>

    <br/>

    <p>Kind regards,<br/>ITSA Support Team</p>",
                    IsBodyHtml = true
                };

                await _emailService.SendEmailAsync(creditNoteEmail);

                return Ok(new { success = true, message = " CreditNote No "+refNo +" Created successfully and sent email to customer" });
            }
            catch (Exception ex)
            {
                //await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }

        }

        [HttpGet("getCreditnotedetailswithrefno")]
        [AllowAnonymous]
        public async Task<IActionResult> getCreditnotedetailswithrefno(int refNo)
        {
            var list = await (from creditnote in _context.CreditNoteHeader
                              join invoice in _context.QuotationInvoice on creditnote.InoviceReferenceNumber equals invoice.InvoiceReferenceNumber
                              join quotation in _context.BillingQuotations on invoice.QuotationReferenceNumber equals quotation.ReferenceNumber
                              join cmp in _context.Companies on invoice.CompanyId equals cmp.Id.ToString()
                              where creditnote.Id == refNo
                              select new
                              {
                                  creditnote,
                                  invoice,
                                  quotation,
                                  cmp
                              }).FirstOrDefaultAsync();

            if (list == null)
                return Ok(new { success = false, message = "Credit Note not found." });


            // ✅ If valid, return full data
            var result = new
            {
                QuotationId = list.quotation.Id,
                ComapanyLogo = list.cmp.CompanyLogo,         
                creditnoteId = list.creditnote.Id,
                QuotationRefno = list.quotation.ReferenceNumber,
                CompanyLogo = list.quotation.Filename,
                ReceivingEntity = list.quotation.ReceivingEntity,
                ReferenceNumber = list.creditnote.CreditNoteReferenceNumber,
                CreditNoteDate = list.creditnote.CreditNoteDate,
                DueDate = list.creditnote.DueDate,
                CompanyWebsite = list.creditnote.CompanyWebsite,
                CompanyName = list.creditnote.CompanyName,
                VatNumber = list.creditnote.VatNumber,
                CompanyEmail = list.creditnote.CompanyEmail,
                CompanyAddress = list.creditnote.CompanyAddress,
                CompanyPhoneNumber = list.creditnote.CompanyPhoneNumber,
                BankAccountNumber = list.creditnote.BankAccountNumber,
                IFSCCode = list.creditnote.IFSCCode,
                BranchCode = list.creditnote.BranchCode,
                BrannchAddress = list.creditnote.BrannchAddress,
                CustomerName = list.creditnote.CustomerName,
                CustomerRefno = list.creditnote.CustomerRefno,
                CustomerEmail = list.creditnote.CustomerEmail,
                CustomerAddress = list.creditnote.CustomerAddress,
                CustomerPhone = list.creditnote.CustomerPhone,
                RegistrationNumber = list.creditnote.RegistrationNumber,
                PaymentTerms = list.creditnote.PaymentTerms,
                InvoiceReferenceNumber = list.invoice.InvoiceReferenceNumber,
                InvoiceStatus = list.invoice.Status,
                InvoiceId = list.invoice.Id,
                InvoiceDate = list.invoice.InvoiceDate,
                Reason = list.invoice.RejectionReason,
                AccountHolderName = list.creditnote.AccountHolderName,
                SubTotal = list.creditnote.SubTotal,
                TotalAmount = list.creditnote.TotalAmount,
                TaxAmount = list.creditnote.TaxAmount,
                QuotationDate = list.quotation.QuotationDate,
                QuotationType = list.quotation.QuotationType,
                Currency = list.quotation.Currency,
                BankName = list.quotation.BankName,

                Details = _context.CreditNoteDetails
                    .Where(d => d.CreditNoteReferenceNumber == list.creditnote.CreditNoteReferenceNumber)
                    .Select(d => new
                    {
                        d.ItemName,
                        d.ItemCode,
                        d.Category,
                        d.UnitRate,
                        d.ItemQuantity,
                        d.Discount,
                        d.DiscountType,
                        d.NatValue,
                        d.VAT
                    }).ToList()
            };

            return Ok(new { list = result });
        }

        [HttpGet("getCreditnotedetailswithInvoicerefno")]
        public async Task<IActionResult> getCreditnotedetailswithInvoicerefno(string refNo)
        {
            var list = await (from creditnote in _context.CreditNoteHeader
                              join invoice in _context.QuotationInvoice on creditnote.InoviceReferenceNumber equals invoice.InvoiceReferenceNumber
                              join quotation in _context.BillingQuotations on invoice.QuotationReferenceNumber equals quotation.ReferenceNumber
                              join cmp in _context.Companies on quotation.CompanyId equals cmp.Id.ToString()
                              where creditnote.InoviceReferenceNumber == refNo
                              select new
                              {
                                  creditnote,
                                  invoice,
                                  quotation,
                                  cmp,
                              }).FirstOrDefaultAsync();

            if (list == null)
                return Ok(new { success = false, message = "Credit Note not found." });


            // ✅ CHECK DUE DATE
            //if (list.creditnote.DueDate != null && list.creditnote.DueDate < DateTime.UtcNow)
            //{
            //    return Ok(new { success = false, message = "Due date has expired. You are not allowed to view this credit note." });

            //}

            // ✅ If valid, return full data
            var result = new
            {
                QuotationId = list.quotation.Id,
                ComapanyLogo = list.cmp.CompanyLogo,
                creditnoteId = list.creditnote.Id,
                QuotationRefno = list.quotation.ReferenceNumber,
                ReferenceNumber = list.creditnote.CreditNoteReferenceNumber,
                CreditNoteDate = list.creditnote.CreditNoteDate,
                DueDate = list.creditnote.DueDate,
                CompanyWebsite = list.creditnote.CompanyWebsite,
                CompanyName = list.creditnote.CompanyName,
                VatNumber = list.creditnote.VatNumber,
                CompanyEmail = list.creditnote.CompanyEmail,
                CompanyAddress = list.creditnote.CompanyAddress,
                CompanyPhoneNumber = list.creditnote.CompanyPhoneNumber,
                BankAccountNumber = list.creditnote.BankAccountNumber,
                IFSCCode = list.creditnote.IFSCCode,
                BranchCode = list.creditnote.BranchCode,
                BrannchAddress = list.creditnote.BrannchAddress,
                CustomerName = list.creditnote.CustomerName,
                CustomerRefno = list.creditnote.CustomerRefno,
                CustomerEmail = list.creditnote.CustomerEmail,
                CustomerAddress = list.creditnote.CustomerAddress,
                CustomerPhone = list.creditnote.CustomerPhone,
                RegistrationNumber = list.creditnote.RegistrationNumber,
                PaymentTerms = list.creditnote.PaymentTerms,
                InvoiceReferenceNumber = list.invoice.InvoiceReferenceNumber,
                InvoiceStatus = list.invoice.Status,
                InvoiceId = list.invoice.Id,
                InvoiceDate = list.invoice.InvoiceDate,
                Reason = list.invoice.RejectionReason,
                AccountHolderName = list.creditnote.AccountHolderName,
                SubTotal = list.creditnote.SubTotal,
                TotalAmount = list.creditnote.TotalAmount,
                TaxAmount = list.creditnote.TaxAmount,
                QuotationDate = list.quotation.QuotationDate,
                QuotationType = list.quotation.QuotationType,
                Currency = list.quotation.Currency,
                ReceivingEntity = list.quotation.ReceivingEntity,
                BankName = list.quotation.BankName,


                Details = _context.CreditNoteDetails
                    .Where(d => d.CreditNoteReferenceNumber == list.creditnote.CreditNoteReferenceNumber)
                    .Select(d => new
                    {
                        d.ItemName,
                        d.ItemCode,
                        d.Category,
                        d.UnitRate,
                        d.ItemQuantity,
                        d.Discount,
                        d.DiscountType,
                        d.NatValue,
                        d.VAT
                    }).ToList()
            };

            return Ok(new { list = result });
        }


        public class CreditNoteDTO
        {
            public CreditNoteHeaderDTO Header { get; set; }
            public List<CreditNoteDetailsDTO> Details { get; set; }
        }

        [HttpGet("getusersListforAudit")]
        public async Task<IActionResult> GetUsersListForAudit()
        {
            try
            {
                var users = await _context.Users
                    .FromSqlRaw("EXEC SP_GetUsersListForAudit")
                    .ToListAsync();

                var result = users.Select(x => new
                {
                    x.Id,
                    x.UserName,
                    x.Email
                }).ToList();

                return Ok(new { list = result });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("CheckStatus")]
        public async Task<IActionResult> CheckStatus()
        {
            await CheckQuotationAndInvoiceStatusAsync();
            return Ok(new { message = "Status updated successfully." });
        }
        private async Task CheckQuotationAndInvoiceStatusAsync()
        {
            var today = DateTime.UtcNow.Date;
            var expiryDate = today.AddDays(-14);

            var quotations = await _context.BillingQuotations
                .Where(q =>
                    (
                        q.Status == "Created" ||
                        q.Status == "Updated" ||
                        q.Status == "Approved" 
                        
                    )
                    &&
                    (
                        q.QuotationDate < expiryDate ||
                        (q.DueDate != null && q.DueDate.Date < today)
                    ))
                .ToListAsync();

            foreach (var quotation in quotations)
            {
                // Expired
                if ((quotation.Status == "Created" ||
                     quotation.Status == "Updated") &&
                    quotation.QuotationDate < expiryDate)
                {
                    quotation.Status = "Expired";

                    await _auditService.AddQuotationAuditwithoutsession(
                        quotation,
                        "Quotation is older than 14 days and has expired."
                    );

                    continue;
                }

                // Overdue
                if ((quotation.Status == "Approved" ||
                     quotation.Status == "Deposit Paid" ||
                     quotation.Status == "Partial Paid") &&
                    quotation.DueDate != null &&
                    quotation.DueDate.Date < today)
                {
                    quotation.Status = "Overdue";

                    await _auditService.AddQuotationAuditwithoutsession(
                        quotation,
                        "Quotation is overdue."
                    );
                }
            }

            await _context.SaveChangesAsync();
        }

        [HttpGet("getCustomersList")]
        public async Task<IActionResult> GetCustomersList()
        {
            List<object> customersList = new();
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var loggedRole = User.FindFirst(ClaimTypes.Role)?.Value;

            int? loggedCompanyId = null;
            var companyIdClaim = User.FindFirst("companyId")?.Value;

            if (int.TryParse(companyIdClaim, out int companyId))
            {
                loggedCompanyId = companyId;
            }
            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            bool isSuperAdmin =
                loggedRole == "Super Admin" ||
                loggedRole == "SuperAdmin";

            if (!isSuperAdmin && loggedCompanyId == null)
            {
                return Unauthorized("CompanyId claim is missing or invalid.");
            }

            using (SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_GetCustomersScreenList", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IsSuperAdmin", isSuperAdmin);
                    cmd.Parameters.AddWithValue("@UserId",loggedUserId);


                    if (loggedCompanyId.HasValue)
                        cmd.Parameters.AddWithValue("@CompanyId", loggedCompanyId.Value);
                    else
                        cmd.Parameters.AddWithValue("@CompanyId", DBNull.Value);

                    await con.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            customersList.Add(new
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                UserName = reader["UserName"]?.ToString(),
                                Name = reader["Name"]?.ToString(),
                                Gender = reader["Gender"]?.ToString(),
                                SurName = reader["SurName"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                Mobile = reader["Mobile"]?.ToString(),
                                Address = reader["Address"]?.ToString(),
                                CustomerRefNo = reader["CustomerRefNo"]?.ToString(),
                                CompanyName = reader["CompanyName"]?.ToString(),
                                CompanyId = Convert.ToInt32(reader["CompanyId"]),
                                Title = reader["Title"]?.ToString(),
                                Status = reader["Status"]?.ToString(),
                            });
                        }
                    }
                }
            }

            return Ok(new { list = customersList });
        }
     
        [HttpPost("savecustomerdetails")]
        public async Task<IActionResult> SaveCustomerDetails([FromBody] CustomersDTO model)
        {
            var loggedEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                // Check if email already exists
                //bool emailExists = await _context.Customers
                //    .AnyAsync(x => x.Email == model.email);

                //if (emailExists)
                //{
                //    return BadRequest(new { message = "Email already exists." });
                //}

                // Get Company VAT Number
                var company = await _context.Companies
                    .FirstOrDefaultAsync(x => x.Id == model.companyId);

                if (company == null)
                {
                    return BadRequest(new { message = "Company not found." });
                }

                // Generate Customer Reference Number
                string customerRefNo = "REF_01";

                var lastCustomer = await _context.Customers
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (lastCustomer != null && !string.IsNullOrEmpty(lastCustomer.CustomerRefNo))
                {
                    int lastNo = Convert.ToInt32(lastCustomer.CustomerRefNo.Replace("REF_", ""));
                    customerRefNo = $"REF_{(lastNo + 1):D2}";
                }

                // Create Customer
                var customer = new Customers
                {
                    GUID = Guid.NewGuid(),
                    CustomerRefNo = customerRefNo,
                    Title = model.title,
                    Name = model.name,
                    Email = model.email,
                    UserName = model.email,     
                    SurName = model.surname,
                    Mobile = model.mobile,
                    Address = model.address,
                    CompanyId = model.companyId,
                    Gender = model.gender,
                    VATNumber = company.VatNumber,
                    Status = "Active",
                    CreatedBy = loggedEmail,
                    ModifiedBy = loggedEmail,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Customer Audit
                await _auditService.AddCustomerAuditAsync(customer, "Customer Saved Successfully");

               

                return Ok(new
                {
                    message = $"Customer Reference No {customerRefNo} saved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message,
                    InnerException = ex.InnerException?.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
        [HttpPost("updatecustomerdetails")]
        public async Task<IActionResult> UpdateCustomerDetails([FromBody] CustomersDTO model)
        {
            var loggedEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                using SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
                using SqlCommand cmd = new SqlCommand("SP_UpdateCustomerDetails", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@Title", model.title);
                cmd.Parameters.AddWithValue("@Name", model.name);
                cmd.Parameters.AddWithValue("@Email", model.email);
                cmd.Parameters.AddWithValue("@SurName", model.surname);
                cmd.Parameters.AddWithValue("@Mobile", model.mobile);
                cmd.Parameters.AddWithValue("@Address", model.address);
                cmd.Parameters.AddWithValue("@CompanyId", model.companyId);
                cmd.Parameters.AddWithValue("@Gender", model.gender);
                cmd.Parameters.AddWithValue("@Status", model.Status);
                cmd.Parameters.AddWithValue("@ModifiedBy", loggedEmail ?? "");
                cmd.Parameters.AddWithValue("@IPAddressOfSystem", ipAddress ?? "Unknown");

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    message = "Customer details updated successfully"
                });
            }
            catch (SqlException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("deletecustomerdetails")]
        public async Task<IActionResult> DeleteCustomerDetails(int Id)
        {
            var loggedEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                using SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString);
                using SqlCommand cmd = new SqlCommand("SP_DeleteCustomerDetails", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Id", Id);
                cmd.Parameters.AddWithValue("@ModifiedBy", loggedEmail ?? "");
                cmd.Parameters.AddWithValue("@IPAddressOfSystem", ipAddress ?? "Unknown");

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    message = "Customer details deleted successfully"
                });
            }
            catch (SqlException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }


        //AUDIT LOGS METHODS

        [HttpGet("GetAuditLogsWithFilter")]
        public async Task<IActionResult> GetAuditLogsWithFilter([FromQuery] string user, [FromQuery] string? screenName, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {

            if (string.IsNullOrWhiteSpace(user))
                return BadRequest("User email is required.");

            var userExists =  _context.Users.Where(x => x.Email == user).FirstOrDefault();
            

            if (userExists == null)
                return BadRequest("User not found.");

            var loggedRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;

            bool isSuperAdmin = loggedRole == "Super Admin" || loggedRole == "SuperAdmin";


            var auditLogs = new List<AuditLogDto>();

            using (SqlConnection con = new SqlConnection(_context.Database.GetConnectionString()))
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("SP_GetAuditLogsWithFilter", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@User", user);
                    cmd.Parameters.AddWithValue("@ScreenName", (object?)screenName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                    //cmd.Parameters.Add(new SqlParameter("@CompanyId",string.IsNullOrEmpty(companyIdClaim) ? DBNull.Value : companyIdClaim));
                    //cmd.Parameters.Add(new SqlParameter("@IsSuperAdmin", isSuperAdmin));

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            auditLogs.Add(new AuditLogDto
                            {
                                Table = reader["Table"]?.ToString(),
                                Id = Convert.ToInt32(reader["Id"]),
                                ActionBy = reader["ActionBy"]?.ToString(),
                                Action = reader["Action"]?.ToString(),
                                ActionDate = Convert.ToDateTime(reader["ActionDate"]),
                                Refno = reader["Refno"] == DBNull.Value
                                    ? null
                                    : reader["Refno"].ToString()
                            });
                        }
                    }
                }
            }

            return Ok(new
            {
                list = auditLogs
            });


        }


        [HttpGet("getAuditLogsWithDetails")]
        public async Task<IActionResult> getAuditLogsWithDetails([FromQuery] string Screenname,[FromQuery] int Id,[FromQuery] string? Refno = null)
        {
            if (string.IsNullOrEmpty(Screenname))
                return BadRequest("Screenname is required.");

            object result;

            switch (Screenname.ToLower())
            {
                case "roles":
                    result = await GetAuditDetails(Id, "Roles", Refno);
                    break;

                case "companies":
                    result = await GetAuditDetails(Id, "Companies", Refno);
                    break;

                case "customers":
                    result = await GetAuditDetails(Id, "Customers", Refno);
                    break;

                case "products":
                    result = await GetAuditDetails(Id, "Products", Refno);
                    break;

                case "quotation":
                    result = await GetAuditDetails(Id, "Quotation", Refno);
                    break;

                case "invoice":

                    string? creditref = Refno;

                    var invoice = await _context.QuotationInvoice
                        .FirstOrDefaultAsync(x => x.InvoiceReferenceNumber == Refno);

                    if (invoice != null && invoice.Status == "Credit note Created")
                    {
                        creditref = invoice.InvoiceReferenceNumber;
                        result = await GetAuditDetails(Id, "Credit", Refno);
                        break;
                    }


                    result = await GetAuditDetails(Id, "Invoice", Refno);
                    break;

                default:
                    return BadRequest("Invalid Screenname.");
            }

            return Ok(new
            {
                screenname = Screenname,
                objectdata = result
            });
        }

        private async Task<object> GetAuditDetails(int id, string type,string? Refno)
        {
            await using var connection = _context.Database.GetDbConnection();

            await connection.OpenAsync();

            await using var command = connection.CreateCommand();

            command.CommandText = "SP_GetAuditDetaills";
            command.CommandType = CommandType.StoredProcedure;

            var idParameter = command.CreateParameter();
            idParameter.ParameterName = "@Id";
            idParameter.Value = id;

            var typeParameter = command.CreateParameter();
            typeParameter.ParameterName = "@Type";
            typeParameter.Value = type;

            var refnoParameter = command.CreateParameter();
            refnoParameter.ParameterName = "@Refno";
            refnoParameter.Value = string.IsNullOrEmpty(Refno)
                ? DBNull.Value
                : Refno;

            command.Parameters.Add(idParameter);
            command.Parameters.Add(typeParameter);
            command.Parameters.Add(refnoParameter);

            await using var reader = await command.ExecuteReaderAsync();

            // Header
            var header = new Dictionary<string, object?>();

            if (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    header[reader.GetName(i)] =
                        reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
            }

            // Details
            var details = new List<Dictionary<string, object?>>();

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    var detail = new Dictionary<string, object?>();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        detail[reader.GetName(i)] =
                            reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }

                    details.Add(detail);
                }
            }

            return new
            {
                header,
                details
            };
        }

        private async Task<List<PaymentUploads>> GetProofOfPayments(string referenceNumber,string type)
        {
            return await _context.PaymentUploads
                .Where(x =>
                    x.ReferenceNumber == referenceNumber &&
                    x.Type == type)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }


        private async Task<EmailMessage> CreateRefundEmail(QuotationInvoice invoice)
        {
            // Get ALL refund proof documents
            var proofOfPayments = await GetProofOfPayments(
                invoice.QuotationReferenceNumber,
                "Refund"
            );

            Console.WriteLine($"Proof of Payments Count: {proofOfPayments?.Count() ?? 0}");

            if (proofOfPayments != null)
            {
                foreach (var proof in proofOfPayments)
                {
                    Console.WriteLine($"Proof File Name: {proof?.Filename}");
                    Console.WriteLine($"Proof File Path: {proof?.Filepath}");
                    Console.WriteLine($"File Exists: {!string.IsNullOrWhiteSpace(proof?.Filepath) && System.IO.File.Exists(proof.Filepath)}");
                    Console.WriteLine("----------------------------------------");
                }
            }

            var email = new EmailMessage
            {
                ToEmail = invoice.CustomerEmail,

                Subject = $"Refund Processed - {invoice.InvoiceReferenceNumber}",

                Body = $@"
        <div style='font-family:Arial, sans-serif; color:#000;'>

            <p>Dear {invoice.CustomerName},</p>

            <p>
                We are pleased to confirm that the refund has been successfully processed.
            </p>

            <p>
                Please find the proof of payment attached for your records.
                Kindly allow the applicable processing time for the funds to reflect in your account.
            </p>

            <p>
                Should you have any questions or require further assistance,
                please do not hesitate to contact us.
            </p>

            <br/>

            <p>
                  Kind regards,<br/>
                  {invoice.CompanyName}<br/>
                  <a href='mailto:{invoice.CompanyEmail}'>
                  {invoice.CompanyEmail}
                   </a>
                </p>

        </div>",

                IsBodyHtml = true,

                Attachments = new List<EmailAttachment>()
            };

            
            // Attach ALL refund proof documents
            foreach (var proof in proofOfPayments)
            {
                if (proof == null)
                    continue;

                // Change FilePath/FileName to your actual column names
                if (!string.IsNullOrWhiteSpace(proof.Filepath))
                {
                    var filePath = proof.Filepath.TrimStart('/', '\\');

                    var physicalFilePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        filePath
                    );

                    Console.WriteLine($"DB Path: {proof.Filepath}");
                    Console.WriteLine($"Physical Path: {physicalFilePath}");
                    Console.WriteLine($"File Exists: {System.IO.File.Exists(physicalFilePath)}");

                    if (System.IO.File.Exists(physicalFilePath))
                    {
                        email.Attachments.Add(new EmailAttachment
                        {
                            Filename = proof.Filename,
                            Filepath = physicalFilePath
                        });

                        Console.WriteLine($"Attachment added: {proof.Filename}");
                    }
                }

            }

            return email;
        }
 //       private EmailMessage CreateInvoiceStatusEmail(QuotationInvoice invoice)
 //       {
 //           bool isCancelled = string.Equals(
 //       invoice.Status,
 //       "Cancelled",
 //       StringComparison.OrdinalIgnoreCase
 //           );

 //           var cancelledDetails = isCancelled
 //               ? $@"
 //<li>
 //                       <strong>Cancelled By:</strong> {invoice.CancelledBy}
 //                   </li>
 //           <li>
 //               <strong>Date of Action:</strong> {invoice.RejectedDate:dd/MM/yyyy}
 //           </li>
 //           <li>
 //               <strong>Reason:</strong> {invoice.RejectionReason}
 //           </li>"
 //               : "";

 //           return new EmailMessage
 //           {
 //               ToEmail = invoice.CustomerEmail,

 //               Subject = $"Invoice Status Update - {invoice.InvoiceReferenceNumber}",

 //               Body = $@"
 //           <div style='font-family:Arial, sans-serif; color:#000;'>

 //               <p>Dear {invoice.CustomerName},</p>

 //               <p>We hope this message finds you well.</p>

 //               <p>
 //                   This is to inform you that invoice
 //                   <strong>{invoice.InvoiceReferenceNumber}</strong>,
 //                   dated {invoice.InvoiceDate:dd/MM/yyyy},
 //                   has been Updated to <strong>{invoice.Status}</strong>.
 //               </p>

 //               <ul>
 //                   <li>
 //                       <strong>Invoice Status:</strong> {invoice.Status}
 //                   </li>

 //                   {cancelledDetails}
 //               </ul>

 //               <br/>

 //               <p>
 //                   Kind Regards,<br/>
 //                   ITSA Support Team<br/>
 //                   <a href='mailto:support@itsa.co.za'>
 //                       support@itsa.co.za
 //                   </a>
 //               </p>

 //           </div>",

 //               IsBodyHtml = true
 //           };
 //       }

private EmailMessage CreateInvoiceStatusEmail(
    QuotationInvoice invoice,
    string? templateUrl)
        {
            bool isCancelled = string.Equals(
                invoice.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase
            );

            var cancelledDetails = isCancelled
                ? $@"
<li>
    <strong>Cancelled By:</strong> {invoice.CancelledBy}
</li>
<li>
    <strong>Date of Action:</strong> {invoice.RejectedDate:dd/MM/yyyy}
</li>
<li>
    <strong>Reason:</strong> {invoice.RejectionReason}
</li>"
                : "";

            // Create receipt URL using the frontend URL received from frontend
            Console.WriteLine($"TemplateUrl received: {templateUrl}");
            var receiptUrl = !string.IsNullOrWhiteSpace(templateUrl)
                ? $"{templateUrl}?invoiceId={invoice.Id}"
                : "";

            // Show receipt link only when order is completed
            var receiptLink = string.Equals(
                invoice.Status,
                "Order Completed",
                StringComparison.OrdinalIgnoreCase
            ) && !string.IsNullOrWhiteSpace(receiptUrl)
                ? $@"
<p>
    Your payment has been completed and your order has been successfully completed.
</p>

<p>
    Please click the link below to view your transaction receipt:
</p>

<p>
    <a href='{receiptUrl}'
       style='color:#15c; text-decoration:underline;'>
        Click here
    </a>
</p>"
                : "";

            return new EmailMessage
            {
                ToEmail = invoice.CustomerEmail,

                Subject = $"Invoice Status Update - {invoice.InvoiceReferenceNumber}",

                Body = $@"
<div style='font-family:Arial, sans-serif; color:#000;'>

    <p>Dear {invoice.CustomerName},</p>

    <p>We hope this message finds you well.</p>

   <p>
    This is to inform you that invoice
    <strong>{invoice.InvoiceReferenceNumber}</strong>,
    dated {invoice.InvoiceDate:dd/MM/yyyy},
    has been {(invoice.Status?.Trim().Equals(
        "Order Completed",
        StringComparison.OrdinalIgnoreCase) == true
        ? "fully paid and Updated to"
        : "Updated to")} <strong>{invoice.Status}</strong>.
</p>

    <ul>
        <li>
            <strong>Invoice Status:</strong> {invoice.Status}
        </li>

        {cancelledDetails}
    </ul>

    {receiptLink}

    <br/>

    <p>
        Kind Regards,<br/>
        ITSA Support Team<br/>
        <a href='mailto:support@itsa.co.za'>
            support@itsa.co.za
        </a>
    </p>

</div>",

                IsBodyHtml = true
            };
        }




        private EmailMessage CreateCompanyStatusEmail(QuotationInvoice invoice)
        {
            bool isCancelled = string.Equals(
        invoice.Status,
        "Cancelled",
        StringComparison.OrdinalIgnoreCase
            );

            var cancelledDetails = isCancelled
                ? $@"
 <li>
                        <strong>Cancelled By:</strong> {invoice.CancelledBy}
                    </li>
            <li>
                <strong>Date of Action:</strong> {invoice.RejectedDate:dd/MM/yyyy}
            </li>
            <li>
                <strong>Reason:</strong> {invoice.RejectionReason}
            </li>"
                : "";

            return new EmailMessage
            {
                ToEmail = invoice.CompanyEmail,

                Subject = $"Invoice Status Update - {invoice.InvoiceReferenceNumber}",

                Body = $@"
            <div style='font-family:Arial, sans-serif; color:#000;'>

                <p>Dear {invoice.CompanyName},</p>

                <p>We hope this message finds you well.</p>

                <p>
                    This is to inform you that invoice
                    <strong>{invoice.InvoiceReferenceNumber}</strong>,
                    dated {invoice.InvoiceDate:dd/MM/yyyy},
                    has been Updated to <strong>{invoice.Status}</strong>.
                </p>

                <ul>
                    <li>
                        <strong>Invoice Status:</strong> {invoice.Status}
                    </li>
                 {cancelledDetails}

                </ul>

                <br/>

                <p>
                    Kind Regards,<br/>
                    ITSA Support Team<br/>
                    <a href='mailto:support@itsa.co.za'>
                        support@itsa.co.za
                    </a>
                </p>

            </div>",

                IsBodyHtml = true
            };
        }

    }


}



