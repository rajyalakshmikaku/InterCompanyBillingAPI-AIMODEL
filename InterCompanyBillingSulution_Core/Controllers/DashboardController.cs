using InterCompanyBillingSulution.DataAcess.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Security.Claims;

namespace InterCompanyBillingSulution_Core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly InterCompanyBillingDbContext _context;
        private readonly IEmailService _emailService;
        private readonly AuditService _auditService;
        public DashboardController(InterCompanyBillingDbContext context, IEmailService emailService, AuditService auditService)
        {
            _context = context;
            _emailService = emailService;
            _auditService = auditService;
        }

        [HttpGet("getQuotationorInvoicecount")]
        public async Task<IActionResult> GetQuotationOrInvoiceCount(int? companyId = null)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            int loggedUserId = 0;

            if (!string.IsNullOrEmpty(userIdClaim) &&
                int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            var loggedRole = User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            bool isSuperAdmin = loggedRole?.Trim().ToLower() == "super admin" || loggedRole?.Trim().ToLower() == "superadmin";


            using SqlConnection con =
                new SqlConnection(_context.Database.GetConnectionString());

            using SqlCommand cmd =
                new SqlCommand("SP_GetQuotationOrInvoiceCount", con);

            cmd.CommandType = CommandType.StoredProcedure;

            // Company ID coming from frontend
            cmd.Parameters.Add("@CompanyId", SqlDbType.Int).Value =
                companyId.HasValue
                    ? companyId.Value
                    : DBNull.Value;

            cmd.Parameters.Add("@IsSuperAdmin", SqlDbType.Bit).Value =
                isSuperAdmin;

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value =
                loggedUserId;
          

            await con.OpenAsync();

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return Ok(new
                {
                    Quotations = new
                    {
                        Total = Convert.ToInt32(reader["QuotationTotal"]),
                        Created = Convert.ToInt32(reader["QuotationCreated"]),
                        Approved = Convert.ToInt32(reader["QuotationApproved"]),
                        Paid = Convert.ToInt32(reader["Paid"]),
                        Deleted = Convert.ToInt32(reader["QuotationDeleted"]),
                        Rejected = Convert.ToInt32(reader["QuotationRejected"]),
                        Expired = Convert.ToInt32(reader["QuotationExpired"]),
                        DepositPaid = Convert.ToInt32(reader["DepositPaid"]),
                        PartialPaid = Convert.ToInt32(reader["PartialPaid"]),
                        Overdue = Convert.ToInt32(reader["QuotationOverdue"]),
                        Unpaid = Convert.ToInt32(reader["UnPaid"])
                    },

                    Invoices = new
                    {
                        Total = Convert.ToInt32(reader["InvoiceTotal"]),
                        Created = Convert.ToInt32(reader["InvoiceCreated"]),
                        ProformaGenerated = Convert.ToInt32(reader["InvoiceProformaGenerated"]),
                        OrderCompleted = Convert.ToInt32(reader["InvoiceCompleted"]),
                        Closed = Convert.ToInt32(reader["InvoiceRejected"]),
                        Refunded = Convert.ToInt32(reader["InvoiceRefunded"]),
                        CreditNote = Convert.ToInt32(reader["InvoiceCreditNote"]),
                                                UnpaidandCreated = Convert.ToInt32(reader["UnpaidandCreated"])

                    }
                });
            }

            return Ok(new
            {
                Quotations = new
                {
                    Total = 0,
                    Created = 0,
                    Approved = 0,
                    Paid = 0,
                    Deleted = 0,
                    Rejected = 0,
                    Expired = 0,
                    Overdue = 0
                },
                Invoices = new
                {
                    Total = 0,
                    Created = 0,
                    ProformaGenerated = 0,
                    OrderCompleted = 0,
                    Closed = 0,
                    Refunded = 0,
                    CreditNote = 0
                }
            });
        }
        [HttpPost("getQuotationorInvoicedetails")]
        public async Task<IActionResult> GetQuotationorInvoicedetails(
    [FromQuery] string type,
    [FromQuery] string status,
    [FromQuery] string companyId)
        {
            var userIdClaim = User.Claims
                .FirstOrDefault(c => c.Type == "userId")?.Value;

            int loggedUserId = 0;

            if (!string.IsNullOrEmpty(userIdClaim) &&
                int.TryParse(userIdClaim, out var id))
            {
                loggedUserId = id;
            }

            var loggedRole = User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            bool isSuperAdmin = loggedRole?.Trim().ToLower() == "super admin" || loggedRole?.Trim().ToLower() == "superadmin";


            List<dynamic> headers = new();
            List<dynamic> details = new();
            List<dynamic> payments = new();

            using SqlConnection con = new SqlConnection(
                _context.Database.GetConnectionString());

            using SqlCommand cmd = new SqlCommand(
                "SP_GetQuotationOrInvoiceDetails",
                con);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Type", type);
            cmd.Parameters.AddWithValue("@Status", status);

            // CompanyId comes from the request, NOT from logged-in user's claim
            cmd.Parameters.AddWithValue(
     "@CompanyId",
     string.IsNullOrWhiteSpace(companyId) ||
     companyId.Equals("null", StringComparison.OrdinalIgnoreCase)
         ? DBNull.Value
         : companyId
 );

            cmd.Parameters.AddWithValue("@IsSuperAdmin", isSuperAdmin);
            cmd.Parameters.AddWithValue("@UserId", loggedUserId);

            await con.OpenAsync();

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            // Header
            while (await reader.ReadAsync())
            {
                headers.Add(new
                {
                    QuotationId =
                        reader["QuotationId"] == DBNull.Value
                            ? (int?)null
                            : Convert.ToInt32(reader["QuotationId"]),

                    QuotationReferenceNumber =
                        reader["QuotationReferenceNumber"] == DBNull.Value
                            ? ""
                            : reader["QuotationReferenceNumber"].ToString(),

                    PorformaReferenceNumber =
                        reader["PorformaReferenceNumber"] == DBNull.Value
                            ? ""
                            : reader["PorformaReferenceNumber"].ToString(),

                    ReferenceNumber =
                        reader["ReferenceNumber"] == DBNull.Value
                            ? ""
                            : reader["ReferenceNumber"].ToString(),

                    QuotationStatus =
                        reader["QuotationStatus"] == DBNull.Value
                            ? ""
                            : reader["QuotationStatus"].ToString(),

                    QuotationDate =
                        reader["QuotationDate"] == DBNull.Value
                            ? (DateTime?)null
                            : Convert.ToDateTime(reader["QuotationDate"]),

                    DueDate =
                        reader["DueDate"] == DBNull.Value
                            ? (DateTime?)null
                            : Convert.ToDateTime(reader["DueDate"]),

                    ReceivingEntity = reader["ReceivingEntity"]?.ToString(),
                    AccountHolderName = reader["AccountHolderName"]?.ToString(),
                    Currency = reader["Currency"]?.ToString(),
                    CompanyName = reader["CompanyName"]?.ToString(),
                    VatNumber = reader["VatNumber"]?.ToString(),
                    CompanyEmail = reader["CompanyEmail"]?.ToString(),
                    CompanyAddress = reader["CompanyAddress"]?.ToString(),
                    CompanyPhoneNumber = reader["CompanyPhoneNumber"]?.ToString(),
                    BankAccountNumber = reader["BankAccountNumber"]?.ToString(),
                    IFSCCode = reader["IFSCCode"]?.ToString(),
                    BranchCode = reader["BranchCode"]?.ToString(),
                    BranchAddress = reader["BranchAddress"]?.ToString(),
                    CustomerName = reader["CustomerName"]?.ToString(),
                    CustomerEmail = reader["CustomerEmail"]?.ToString(),
                    CustomerAddress = reader["CustomerAddress"]?.ToString(),
                    CustomerPhone = reader["CustomerPhone"]?.ToString(),
                    CompanyLogo = reader["CompanyLogo"]?.ToString(),
                    RegistrationNumber = reader["RegistrationNumber"]?.ToString(),
                    QuotationType = reader["QuotationType"]?.ToString(),
                    PaymentTerms = reader["PaymentTerms"]?.ToString(),
                    Reason = reader["Reason"]?.ToString(),

                    SubTotal =
                        reader["SubTotal"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["SubTotal"]),

                    TotalAmount =
                        reader["TotalAmount"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["TotalAmount"]),

                    BankName = reader["BankName"]?.ToString(),
                    Status = reader["Status"]?.ToString(),

                    InvoiceId =
                        type == "Invoice"
                            ? (
                                reader["QuotationId"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(reader["QuotationId"])
                              )
                            : 0,

                    InvoiceReferenceNumber =
                        type == "Invoice"
                            ? reader["InvoiceReferenceNumber"]?.ToString()
                            : null,

                    InvoiceStatus =
                        type == "Invoice"
                            ? reader["InvoiceStatus"]?.ToString()
                            : null,

                    InvoiceDate =
                        type == "Invoice" &&
                        reader["InvoiceDate"] != DBNull.Value
                            ? Convert.ToDateTime(reader["InvoiceDate"])
                            : (DateTime?)null,

                    PaidAmount =
                        reader["PaidAmount"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["PaidAmount"]),

                    OutstandingAmount =
                        reader["OutstandingAmount"] == DBNull.Value
                          ? 0
                       : Convert.ToDecimal(reader["OutstandingAmount"]),

                });
            }

            await reader.NextResultAsync();

            // Details
            while (await reader.ReadAsync())
            {
                details.Add(new
                {
                    ReferenceNumber =
                        reader["ReferenceNumber"]?.ToString(),

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

                    Description = reader["Description"]?.ToString(),
                    Category = reader["Category"]?.ToString(),
                    ItemCode = reader["ItemCode"]?.ToString(),
                    ItemName = reader["ItemName"]?.ToString(),

                    Discount =
                        reader["Discount"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["Discount"]),

                    DiscountType = reader["DiscountType"]?.ToString(),

                    Tax =
                        reader["VAT"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["VAT"])
                });
            }
            bool hasPayments = await reader.NextResultAsync();

            Console.WriteLine($"Payments result set exists: {hasPayments}");


            // =========================
            // Result Set 3: Payments
            // =========================
            if (hasPayments)
            {
                while (await reader.ReadAsync())
                {
                    payments.Add(new
                    {
                        PaymentId = reader["PaymentId"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["PaymentId"]),

                        QuotationRefNo = reader["QuotationRefNo"]?.ToString(),

                        TransactionId = reader["TransactionId"]?.ToString(),

                        PaymentDate = reader["PaymentDate"] == DBNull.Value
                            ? (DateTime?)null
                            : Convert.ToDateTime(reader["PaymentDate"]),

                        PaidAmount = reader["PaidAmount"] == DBNull.Value
                            ? 0m
                            : Convert.ToDecimal(reader["PaidAmount"]),

                        PaymentMethod = reader["PaymentMethod"]?.ToString(),

                        TotalAmount = reader["TotalAmount"] == DBNull.Value
                            ? 0m
                            : Convert.ToDecimal(reader["TotalAmount"]),

                        TotatlDue = reader["TotatlDue"] == DBNull.Value
                            ? 0m
                            : Convert.ToDecimal(reader["TotatlDue"])
                    });
                }
            }

            Console.WriteLine($"Payments fetched: {payments.Count}");

            Console.WriteLine($"Payments fetched: {payments.Count}");
            var result = headers.Select(h => new
            {
                Header = h,

                Detail = details
                    .Where(d =>
                        d.ReferenceNumber ==
                        (
                            type == "Invoice"
                                ? h.InvoiceReferenceNumber
                                : h.ReferenceNumber
                        ))
                    .ToList(),
                PaymentHistory = payments.Where(p => p.QuotationRefNo == h.ReferenceNumber).ToList()

            });

            return Ok(new
            {
                Type = type,
                CompanyId = companyId,
                Count = headers.Count,
                List = result
            });
        }
        [HttpGet("GetLatestStatusRecords")]

        public async Task<IActionResult> GetLatestStatusRecords(int? companyId)
        {
            try
            {
                var loggedRole = User.Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                bool isSuperAdmin = loggedRole?.Trim().ToLower() == "super admin" || loggedRole?.Trim().ToLower() == "superadmin";


                Console.WriteLine($"Selected Company ID from Frontend: {companyId}");

                var result = new List<object>();

                using (var connection = _context.Database.GetDbConnection())
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SP_GetLatestStatusRecords";
                        command.CommandType = CommandType.StoredProcedure;

                        // CompanyId from frontend
                        var companyParameter = new SqlParameter(
                            "@CompanyId",
                            SqlDbType.NVarChar,
                            50
                        )
                        {
                            Value = companyId.HasValue
                                ? companyId.Value.ToString()
                                : DBNull.Value
                        };

                        command.Parameters.Add(companyParameter);

                        // IsSuperAdmin
                        command.Parameters.Add(
                            new SqlParameter("@IsSuperAdmin", SqlDbType.Bit)
                            {
                                Value = isSuperAdmin
                            }
                        );

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new
                                {
                                    Module = reader["Module"] != DBNull.Value
                                        ? reader["Module"].ToString()
                                        : null,

                                    Id = reader["Id"] != DBNull.Value
                                        ? Convert.ToInt32(reader["Id"])
                                        : 0,

                                    ReferenceNumber = reader["ReferenceNumber"] != DBNull.Value
                                        ? reader["ReferenceNumber"].ToString()
                                        : null,

                                    CompanyId = reader["CompanyId"] != DBNull.Value
                                        ? Convert.ToInt32(reader["CompanyId"])
                                        : 0,

                                    CompanyName = reader["CompanyName"] != DBNull.Value
                                        ? reader["CompanyName"].ToString()
                                        : null,

                                    CustomerName = reader["CustomerName"] != DBNull.Value
                                        ? reader["CustomerName"].ToString()
                                        : null,

                                    Status = reader["Status"] != DBNull.Value
                                        ? reader["Status"].ToString()
                                        : null,

                                    CreatedDate = reader["CreatedDate"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["CreatedDate"])
                                        : (DateTime?)null,

                                    ModifiedDate = reader["ModifiedDate"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["ModifiedDate"])
                                        : (DateTime?)null,

                                    ModifiedBy = reader["ModifiedBy"] != DBNull.Value
                                        ? reader["ModifiedBy"].ToString()
                                        : null
                                });
                            }
                        }
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = ex.Message,
                    Details = ex.InnerException?.Message
                });
            }
        }
    }
}
