using InterCompanyBillingSulution.DataAcess.Data;
using InterCompanyBillingSulution.Models;
using InterCompanyBillingSulution.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;


namespace InterCompanyBillingSulution_Core.Controllers
{
    [Authorize]

    public class CompaniesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        private readonly InterCompanyBillingDbContext _context;
        private readonly AuditService _auditService;
        private readonly IConfiguration _configuration;

        public CompaniesController(InterCompanyBillingDbContext context, AuditService auditService, IConfiguration configuration)
        {
            _context = context;
            _auditService = auditService;
            _configuration = configuration;

        }
        

        [HttpPost("deletecompanies/{id}")]
        public async Task<IActionResult> DeleteCompanies(int id)
        {
            var loggedEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            using SqlConnection con = new(_context.Database.GetDbConnection().ConnectionString);
            using SqlCommand cmd = new("SP_DeleteCompanies", con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@ModifiedBy", loggedEmail);
            cmd.Parameters.AddWithValue("@ActionBy", loggedEmail);
            cmd.Parameters.AddWithValue("@IPAddress", ip ?? "");

            await con.OpenAsync();

            string message = "";
            string filePath = "";

            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    if (reader["Message"] != DBNull.Value)
                        message = reader["Message"].ToString();

                    if (reader["FilePath"] != DBNull.Value)
                        filePath = reader["FilePath"].ToString();
                }
            }

            if (message == "Company not found.")
                return NotFound(new { message });

            // Delete physical logo file
            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            return Ok(new { message });

        }
        [HttpGet("GetRejectedQuotationInvoiceList")]
        public async Task<IActionResult> GetRejectedQuotationInvoiceList(string type,string status)
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
            List<QutationInvioceDto> documents = new();
            List<QuotationInvoiceDetailDto> details = new();
            List<QuotationPaymentDto> payments = new();

            using (SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_GetQuotationInvoiceListByTypeAndStatus", con))
                {
                   
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IsSuperAdmin", isSuperAdmin);
                    cmd.Parameters.AddWithValue("@UserId", loggedUserId);
                    cmd.Parameters.AddWithValue("@Type", type);
                    cmd.Parameters.AddWithValue("@Status", status);

                    await con.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        // ============================
                        // Result Set 1 - Header
                        // ============================
                        while (await reader.ReadAsync())
                        {
                            documents.Add(new QutationInvioceDto
                            {
                                DocumentType = reader["DocumentType"]?.ToString(),
                                QuotationId = reader["Id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Id"]),
                                InvoiceId = reader["Id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Id"]),

                                QuotationReferenceNumber = reader["ReferenceNumber"]?.ToString(),
                                InvoiceReferenceNumber = reader["InvoiceReferenceNumber"]?.ToString(),

                                QuotationDate = reader["QuotationDate"] == DBNull.Value
                                    ? DateTime.MinValue
                                    : Convert.ToDateTime(reader["QuotationDate"]),

                                InvoiceDate = reader["InvoiceDate"] == DBNull.Value
                                    ? DateTime.MinValue
                                    : Convert.ToDateTime(reader["InvoiceDate"]),

                                QuotationType = reader["QuotationType"]?.ToString(),
                                ReceivingEntity = reader["ReceivingEntity"]?.ToString(),
                                Currency = reader["Currency"]?.ToString(),
                                Status = reader["Status"]?.ToString(),

                                CompanyName = reader["CompanyName"]?.ToString(),
                                VatNumber = reader["VatNumber"]?.ToString(),
                                RegistrationNumber = reader["RegistrationNumber"]?.ToString(),
                                CompanyAddress = reader["CompanyAddress"]?.ToString(),
                                CompanyWebsite = reader["CompanyWebsite"]?.ToString(),
                                CompanyEmail = reader["CompanyEmail"]?.ToString(),
                                CompanyPhoneNumber = reader["CompanyPhoneNumber"]?.ToString(),

                                CustomerName = reader["CustomerName"]?.ToString(),
                                CustomerPhone = reader["CustomerPhone"]?.ToString(),
                                CustomerEmail = reader["CustomerEmail"]?.ToString(),
                                CustomerAddress = reader["CustomerAddress"]?.ToString(),

                                BankAccountNumber = reader["BankAccountNumber"]?.ToString(),
                                BranchCode = reader["BranchCode"]?.ToString(),
                                BranchAddress = reader["BranchAddress"]?.ToString(),
                                IFSCCode = reader["IFSCCode"]?.ToString(),
                                AccountHolderName = reader["AccountHolderName"]?.ToString(),
                                PaymentTerms = reader["PaymentTerms"]?.ToString(),
                                BankName = reader["BankName"]?.ToString(),

                                Reason = reader["RejectionReason"]?.ToString(),
                                RejectedDate = reader["RejectedDate"] == DBNull.Value
                                    ? DateTime.MinValue
                                    : Convert.ToDateTime(reader["RejectedDate"]),

                                DueDate = reader["DueDate"] == DBNull.Value
                                    ? DateTime.MinValue
                                    : Convert.ToDateTime(reader["DueDate"]),

                                SubTotal = reader["SubTotal"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SubTotal"]),
                                TaxAmount = reader["TaxAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TaxAmount"]),
                                TotalAmount = reader["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalAmount"]),
                                PorformaReferenceNumber = reader["PorformaReferenceNumber"] == DBNull.Value ? null : reader["PorformaReferenceNumber"].ToString(),

                                Detail = new List<QuotationInvoiceDetailDto>()
                            });
                        }

                        // ============================
                        // Result Set 2 - Details
                        // ============================

                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                details.Add(new QuotationInvoiceDetailDto
                                {
                                    ReferenceNumber = reader["ReferenceNumber"]?.ToString(),
                                    DetailId = reader["DetailId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DetailId"]),
                                    InventoryId = reader["InventoryId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InventoryId"]),

                                    ItemName = reader["ItemName"]?.ToString(),
                                    ItemCode = reader["ItemCode"]?.ToString(),
                                    Category = reader["Category"]?.ToString(),
                                    Description = reader["Description"]?.ToString(),

                                    QuotationQuantity = reader["QuotationQuantity"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QuotationQuantity"]),
                                    UnitRate = reader["UnitRate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["UnitRate"]),
                                    QuotationAmount = reader["QuotationAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QuotationAmount"]),
                                    Tax = reader["VAT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["VAT"]),
                                    Discount = reader["Discount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Discount"]),
                                    DiscountType = reader["DiscountType"]?.ToString(),
                                    //DiscountValue = reader["DiscountValue"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DiscountValue"])
                                });
                            }
                            await reader.NextResultAsync();

                           
                                while (await reader.ReadAsync())
                                {
                                    payments.Add(new QuotationPaymentDto
                                    {
                                        PaymentId = reader["PaymentId"] == DBNull.Value
                                            ? 0
                                            : Convert.ToInt32(reader["PaymentId"]),

                                        QuotationRefNo = reader["QuotationRefNo"] == DBNull.Value
                                            ? null
                                            : reader["QuotationRefNo"].ToString(),

                                        TransactionId = reader["TransactionId"] == DBNull.Value
                                            ? null
                                            : reader["TransactionId"].ToString(),

                                        PaymentDate = reader["PaymentDate"] == DBNull.Value ? DateTime.MinValue: Convert.ToDateTime(reader["PaymentDate"]),

                                        PaidAmount = reader["PaidAmount"] == DBNull.Value
                                            ? 0
                                            : Convert.ToDecimal(reader["PaidAmount"]),

                                        

                                        TotalAmount = reader["TotalAmount"] == DBNull.Value
                                            ? 0
                                            : Convert.ToDecimal(reader["TotalAmount"]),

                                        TotatlDue = reader["TotatlDue"] == DBNull.Value
                                            ? 0
                                            : Convert.ToDecimal(reader["TotatlDue"])
                                    });
                                }
                            
                        }
                    }
                }
            }

            // ============================
            // Map Details to Header
            // ============================

            foreach (var doc in documents)
            {
                string referenceNo = doc.DocumentType == "Invoice"
                    ? doc.InvoiceReferenceNumber
                    : doc.QuotationReferenceNumber;

                doc.Detail = details.Where(x => x.ReferenceNumber == referenceNo).ToList();
                doc.PaymentHistory = payments
                       .Where(x => x.QuotationRefNo == referenceNo)
                       .ToList();
            }


            return Ok(new
            {
                list = documents,

            });
        }
        //[HttpGet("GetRejectedQuotationInvoiceList")]
        //public async Task<IActionResult> GetRejectedQuotationInvoiceList()
        //{
        //    List<QutationInvioceDto> documents = new();
        //    List<QuotationInvoiceDetailDto> details = new();

        //    using (SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
        //    {
        //        using (SqlCommand cmd = new SqlCommand("SP_GetRejectedQuotationInvoiceList", con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@IsSuperAdmin", 1);

        //            await con.OpenAsync();

        //            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        //            {
        //                // ============================
        //                // Result Set 1 - Header
        //                // ============================
        //                while (await reader.ReadAsync())
        //                {
        //                    documents.Add(new QutationInvioceDto
        //                    {
        //                        DocumentType = reader["DocumentType"]?.ToString(),
        //                        QuotationId = reader["Id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Id"]),
        //                        InvoiceId = reader["Id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Id"]),

        //                        QuotationReferenceNumber = reader["ReferenceNumber"]?.ToString(),
        //                        InvoiceReferenceNumber = reader["InvoiceReferenceNumber"]?.ToString(),

        //                        QuotationDate = reader["QuotationDate"] == DBNull.Value
        //                            ? DateTime.MinValue
        //                            : Convert.ToDateTime(reader["QuotationDate"]),

        //                        InvoiceDate = reader["InvoiceDate"] == DBNull.Value
        //                            ? DateTime.MinValue
        //                            : Convert.ToDateTime(reader["InvoiceDate"]),

        //                        QuotationType = reader["QuotationType"]?.ToString(),
        //                        ReceivingEntity = reader["ReceivingEntity"]?.ToString(),
        //                        Currency = reader["Currency"]?.ToString(),
        //                        Status = reader["Status"]?.ToString(),

        //                        CompanyName = reader["CompanyName"]?.ToString(),
        //                        VatNumber = reader["VatNumber"]?.ToString(),
        //                        RegistrationNumber = reader["RegistrationNumber"]?.ToString(),
        //                        CompanyAddress = reader["CompanyAddress"]?.ToString(),
        //                        CompanyWebsite = reader["CompanyWebsite"]?.ToString(),
        //                        CompanyEmail = reader["CompanyEmail"]?.ToString(),
        //                        CompanyPhoneNumber = reader["CompanyPhoneNumber"]?.ToString(),

        //                        CustomerName = reader["CustomerName"]?.ToString(),
        //                        CustomerPhone = reader["CustomerPhone"]?.ToString(),
        //                        CustomerEmail = reader["CustomerEmail"]?.ToString(),
        //                        CustomerAddress = reader["CustomerAddress"]?.ToString(),

        //                        BankAccountNumber = reader["BankAccountNumber"]?.ToString(),
        //                        BranchCode = reader["BranchCode"]?.ToString(),
        //                        BranchAddress = reader["BranchAddress"]?.ToString(),
        //                        IFSCCode = reader["IFSCCode"]?.ToString(),
        //                        AccountHolderName = reader["AccountHolderName"]?.ToString(),
        //                        PaymentTerms = reader["PaymentTerms"]?.ToString(),
        //                        BankName = reader["BankName"]?.ToString(),

        //                        Reason = reader["RejectionReason"]?.ToString(),
        //                        RejectedDate = reader["RejectedDate"] == DBNull.Value
        //                            ? DateTime.MinValue
        //                            : Convert.ToDateTime(reader["RejectedDate"]),

        //                        DueDate = reader["DueDate"] == DBNull.Value
        //                            ? DateTime.MinValue
        //                            : Convert.ToDateTime(reader["DueDate"]),

        //                        SubTotal = reader["SubTotal"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SubTotal"]),
        //                        TaxAmount = reader["TaxAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TaxAmount"]),
        //                        TotalAmount = reader["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalAmount"]),
        //                        PorformaReferenceNumber = reader["PorformaReferenceNumber"] == DBNull.Value ? null : reader["PorformaReferenceNumber"].ToString(),

        //                        Detail = new List<QuotationInvoiceDetailDto>()
        //                    });
        //                }

        //                // ============================
        //                // Result Set 2 - Details
        //                // ============================

        //                if (await reader.NextResultAsync())
        //                {
        //                    while (await reader.ReadAsync())
        //                    {
        //                        details.Add(new QuotationInvoiceDetailDto
        //                        {
        //                            ReferenceNumber = reader["ReferenceNumber"]?.ToString(),
        //                            DetailId = reader["DetailId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DetailId"]),
        //                            InventoryId = reader["InventoryId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InventoryId"]),

        //                            ItemName = reader["ItemName"]?.ToString(),
        //                            ItemCode = reader["ItemCode"]?.ToString(),
        //                            Category = reader["Category"]?.ToString(),
        //                            Description = reader["Description"]?.ToString(),

        //                            QuotationQuantity = reader["QuotationQuantity"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QuotationQuantity"]),
        //                            UnitRate = reader["UnitRate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["UnitRate"]),
        //                            QuotationAmount = reader["QuotationAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QuotationAmount"]),
        //                            Tax = reader["VAT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["VAT"]),
        //                            Discount = reader["Discount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Discount"]),
        //                            DiscountType = reader["DiscountType"]?.ToString(),
        //                            //DiscountValue = reader["DiscountValue"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DiscountValue"])
        //                        });
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    // ============================
        //    // Map Details to Header
        //    // ============================

        //    foreach (var doc in documents)
        //    {
        //        string referenceNo = doc.DocumentType == "Invoice"
        //            ? doc.InvoiceReferenceNumber
        //            : doc.QuotationReferenceNumber;

        //        doc.Detail = details
        //            .Where(x => x.ReferenceNumber == referenceNo)
        //            .ToList();
        //    }

        //    return Ok(new
        //    {
        //        list = documents
        //    });
        //}
        [HttpGet("getEntitiesList")]
        public async Task<IActionResult> GetEntitiesList()
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var companyId = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            int loggedUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            bool isSuperAdmin = role == "Super Admin" || role == "SuperAdmin";
            try
            {
                List<Companies> companies = new();

                using SqlConnection con = new(_context.Database.GetDbConnection().ConnectionString);
                using SqlCommand cmd = new("SP_GetCompaniesScreenList", con);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IsSuperAdmin", isSuperAdmin);
                cmd.Parameters.AddWithValue("@UserId", loggedUserId);

                await con.OpenAsync();

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    companies.Add(new Companies
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        CompanyName = reader["CompanyName"]?.ToString(),
                        CompanyAddress = reader["CompanyAddress"]?.ToString(),
                        CompanyPhoneNumber = reader["CompanyPhoneNumber"]?.ToString(),
                        CompanyEmail = reader["CompanyEmail"]?.ToString(),
                        VatNumber = reader["VatNumber"]?.ToString(),
                        RegistrationNumber = reader["RegistrationNumber"]?.ToString(),
                        CompanyWebsite = reader["CompanyWebsite"]?.ToString(),
                        CompanyLogo = reader["CompanyLogo"]?.ToString(),
                        FilePath = reader["FilePath"]?.ToString(),
                        BankAccountNumber = reader["BankAccountNumber"]?.ToString(),
                        BranchCode = reader["BranchCode"]?.ToString(),
                        BranchAddress = reader["BranchAddress"]?.ToString(),
                        IFSCCode = reader["IFSCCode"]?.ToString(),
                        AccountHolderName = reader["AccountHolderName"]?.ToString(),
                        CreatedBy = reader["CreatedBy"]?.ToString(),
                        CreatedDate = reader["CreatedDate"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(reader["CreatedDate"]),
                        ModifiedBy = reader["ModifiedBy"]?.ToString(),
                        ModifiedDate = reader["ModifiedDate"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(reader["ModifiedDate"]),
                        Status = reader["Status"]?.ToString(),
                        BankName = reader["BankName"]?.ToString()
                    });
                }

                return Ok(new
                {
                    list = companies
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Error = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }

        //[HttpGet("getEntitiesList")]
        //public async Task<IActionResult> GetEntitiesList()
        //{
        //    try
        //    {
        //        List<Companies> companies = new();

        //        using SqlConnection con = new(_context.Database.GetDbConnection().ConnectionString);
        //        using SqlCommand cmd = new("SP_GetCompaniesScreenList", con);

        //        cmd.CommandType = CommandType.StoredProcedure;

        //        await con.OpenAsync();

        //        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

        //        while (await reader.ReadAsync())
        //        {
        //            companies.Add(new Companies
        //            {
        //                Id = Convert.ToInt32(reader["Id"]),
        //                CompanyName = reader["CompanyName"]?.ToString(),
        //                CompanyAddress = reader["CompanyAddress"]?.ToString(),
        //                CompanyPhoneNumber = reader["CompanyPhoneNumber"]?.ToString(),
        //                CompanyEmail = reader["CompanyEmail"]?.ToString(),
        //                VatNumber = reader["VatNumber"]?.ToString(),
        //                RegistrationNumber = reader["RegistrationNumber"]?.ToString(),
        //                CompanyWebsite = reader["CompanyWebsite"]?.ToString(),
        //                CompanyLogo = reader["CompanyLogo"]?.ToString(),
        //                FilePath = reader["FilePath"]?.ToString(),
        //                BankAccountNumber = reader["BankAccountNumber"]?.ToString(),
        //                BranchCode = reader["BranchCode"]?.ToString(),
        //                BranchAddress = reader["BranchAddress"]?.ToString(),
        //                IFSCCode = reader["IFSCCode"]?.ToString(),
        //                AccountHolderName = reader["AccountHolderName"]?.ToString(),
        //                CreatedBy = reader["CreatedBy"]?.ToString(),
        //                CreatedDate = reader["CreatedDate"] == DBNull.Value
        //                    ? DateTime.MinValue
        //                    : Convert.ToDateTime(reader["CreatedDate"]),
        //                ModifiedBy = reader["ModifiedBy"]?.ToString(),
        //                ModifiedDate = reader["ModifiedDate"] == DBNull.Value
        //                    ? DateTime.MinValue
        //                    : Convert.ToDateTime(reader["ModifiedDate"]),
        //                Status = reader["Status"]?.ToString(),
        //                BankName = reader["BankName"]?.ToString()
        //            });
        //        }

        //        return Ok(new
        //        {
        //            list = companies
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new
        //        {
        //            Error = ex.Message,
        //            Inner = ex.InnerException?.Message
        //        });
        //    }
        //}
        [HttpPost("savecompanies")]
        public async Task<IActionResult> SaveCompanies([FromForm] CompaniesDTO companiesDto, IFormFile? companyLogo)
        {
            try
            {
                var loggedEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

                if (companiesDto == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Companies data is empty."
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed.",
                        errors = ModelState.Values
                                           .SelectMany(v => v.Errors)
                                           .Select(e => e.ErrorMessage)
                    });
                }

                string filePath = "";
                string fileName = "";

                // Upload Logo
                if (companyLogo != null && companyLogo.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    fileName = $"{Guid.NewGuid()}_{Path.GetFileName(companyLogo.FileName)}";
                    filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await companyLogo.CopyToAsync(stream);
                    }
                }

                using SqlConnection con = new(_context.Database.GetDbConnection().ConnectionString);
                using SqlCommand cmd = new("SP_SaveCompanies", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@CompanyName", companiesDto.CompanyName);
                cmd.Parameters.AddWithValue("@CompanyAddress", companiesDto.CompanyAddress);
                cmd.Parameters.AddWithValue("@CompanyPhoneNumber", companiesDto.CompanyPhoneNumber);
                cmd.Parameters.AddWithValue("@CompanyEmail", companiesDto.CompanyEmail);
                cmd.Parameters.AddWithValue("@VatNumber", companiesDto.VatNumber);
                cmd.Parameters.AddWithValue("@RegistrationNumber", companiesDto.RegistrationNumber);
                cmd.Parameters.AddWithValue("@CompanyWebsite", companiesDto.CompanyWebsite ?? "");
                cmd.Parameters.AddWithValue("@CompanyLogo", fileName);
                cmd.Parameters.AddWithValue("@FilePath", filePath);
                cmd.Parameters.AddWithValue("@BankAccountNumber", companiesDto.BankAccountNumber);
                cmd.Parameters.AddWithValue("@BranchCode", companiesDto.BranchCode);
                cmd.Parameters.AddWithValue("@BranchAddress", companiesDto.BranchAddress);
                cmd.Parameters.AddWithValue("@IFSCCode", "0");
                cmd.Parameters.AddWithValue("@AccountHolderName", companiesDto.AccountHolderName);
                cmd.Parameters.AddWithValue("@CreatedBy", loggedEmail);
                cmd.Parameters.AddWithValue("@BankName", companiesDto.BankName);
                cmd.Parameters.AddWithValue("@ActionBy", loggedEmail);
                cmd.Parameters.AddWithValue("@IPAddress", ip);

                await con.OpenAsync();

                string message = "";

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        message = reader["Message"]?.ToString() ?? "";
                    }
                }

                // Duplicate record checks
                if (message == "Company name already exists." ||
                    message == "Company email already exists." ||
                    message == "Registration Number already exists." ||
                    message == "Account Number already exists.")
                {
                    return Conflict(new
                    {
                        success = false,
                        message
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = string.IsNullOrEmpty(message)
                        ? "Company saved successfully."
                        : message
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "A database error occurred.",
                    error = ex.Message
                });
            }
            catch (IOException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "An error occurred while uploading the company logo.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
            }
        }
        [HttpPost("updatecompanies/{id}")]
        public async Task<IActionResult> UpdateCompanies(int id, [FromForm] CompaniesDTO companiesDto, IFormFile? companyLogo)
        {
            try
            {
                var loggedEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

                if (companiesDto == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Companies data is empty."
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed.",
                        errors = ModelState.Values
                                           .SelectMany(v => v.Errors)
                                           .Select(e => e.ErrorMessage)
                    });
                }

                string fileName = string.Empty;
                string filePath = string.Empty;

                using SqlConnection con = new(_context.Database.GetDbConnection().ConnectionString);
                await con.OpenAsync();

                // Get existing logo details
                using (SqlCommand getCmd = new SqlCommand("SELECT CompanyLogo, FilePath FROM Companies WHERE Id=@Id", con))
                {
                    getCmd.Parameters.AddWithValue("@Id", id);

                    using SqlDataReader reader = await getCmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                    {
                        return NotFound(new
                        {
                            success = false,
                            message = "Company not found."
                        });
                    }

                    fileName = reader["CompanyLogo"]?.ToString() ?? "";
                    filePath = reader["FilePath"]?.ToString() ?? "";
                }

                // Upload new logo if selected
                if (companyLogo != null && companyLogo.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    fileName = $"{Guid.NewGuid()}_{Path.GetFileName(companyLogo.FileName)}";
                    filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await companyLogo.CopyToAsync(stream);
                    }
                }

                using SqlCommand cmd = new("SP_UpdateCompanies", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@CompanyName", companiesDto.CompanyName ?? "");
                cmd.Parameters.AddWithValue("@CompanyAddress", companiesDto.CompanyAddress ?? "");
                cmd.Parameters.AddWithValue("@CompanyPhoneNumber", companiesDto.CompanyPhoneNumber ?? "");
                cmd.Parameters.AddWithValue("@CompanyEmail", companiesDto.CompanyEmail ?? "");
                cmd.Parameters.AddWithValue("@VatNumber", companiesDto.VatNumber ?? "");
                cmd.Parameters.AddWithValue("@RegistrationNumber", companiesDto.RegistrationNumber ?? "");
                cmd.Parameters.AddWithValue("@CompanyWebsite", companiesDto.CompanyWebsite ?? "");
                cmd.Parameters.AddWithValue("@CompanyLogo", fileName);
                cmd.Parameters.AddWithValue("@FilePath", filePath);
                cmd.Parameters.AddWithValue("@BankAccountNumber", companiesDto.BankAccountNumber ?? "");
                cmd.Parameters.AddWithValue("@BranchCode", companiesDto.BranchCode ?? "");
                cmd.Parameters.AddWithValue("@BranchAddress", companiesDto.BranchAddress ?? "");
                cmd.Parameters.AddWithValue("@IFSCCode", companiesDto.IFSCCode ?? "");
                cmd.Parameters.AddWithValue("@AccountHolderName", companiesDto.AccountHolderName ?? "");
                cmd.Parameters.AddWithValue("@ModifiedBy", loggedEmail);
                cmd.Parameters.AddWithValue("@Status", companiesDto.Status ?? "Active");
                cmd.Parameters.AddWithValue("@BankName", companiesDto.BankName ?? "");
                cmd.Parameters.AddWithValue("@ActionBy", loggedEmail);
                cmd.Parameters.AddWithValue("@IPAddress", ip);

                string message = "";

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        message = reader["Message"]?.ToString() ?? "";
                    }
                }

                switch (message)
                {
                    case "Company not found.":
                        return NotFound(new
                        {
                            success = false,
                            message
                        });

                    case "Company name already exists.":
                    case "Company email already exists.":
                    case "Registration Number already exists.":
                    case "Account Number already exists.":
                        return Conflict(new
                        {
                            success = false,
                            message
                        });

                    case "No changes found.":
                        return BadRequest(new
                        {
                            success = false,
                            message
                        });

                    case "Company updated successfully.":
                        return Ok(new
                        {
                            success = true,
                            message
                        });

                    default:
                        return Ok(new
                        {
                            success = true,
                            message = string.IsNullOrWhiteSpace(message)
                                ? "Company updated successfully."
                                : message
                        });
                }
            }
            catch (SqlException ex)
            {
                // Log ex using ILogger

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "A database error occurred.",
                    error = ex.Message
                });
            }
            catch (IOException ex)
            {
                // Log ex using ILogger

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "An error occurred while uploading the company logo.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                // Log ex using ILogger

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
            }
        }
    



    }
}
