using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InterCompanyBillingSulution.DataAcess.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Claims;
using InterCompanyBillingSulution.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using InterCompanyBillingSulution.Models.DTO;

namespace InterCompanyBillingSulution_Core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly InterCompanyBillingDbContext _context;

        public ReportsController(InterCompanyBillingDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(DateTime? fromDate)
        {
            if (!fromDate.HasValue)
            {
                return BadRequest("From date is required.");
            }

            var loggedRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var loggedCompanyId = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;

            bool isSuperAdmin = loggedRole == "Super Admin" || loggedRole == "SuperAdmin";

            // Start date from frontend
            var startDate = fromDate.Value.Date;

            // End date = today (exclusive so today's records are included)
            var endDate = DateTime.Today.AddDays(1);

            // Quotations
            IQueryable<BillingQuotation> quotationsQuery = _context.BillingQuotations
                .Where(x => x.CreatedDate >= startDate &&
                            x.CreatedDate < endDate);

            // Invoices
            IQueryable<InvoiceDashboardDto> invoiceQuery =
                from invoice in _context.QuotationInvoice
                join quotation in _context.BillingQuotations
                    on invoice.QuotationReferenceNumber equals quotation.ReferenceNumber
                where invoice.CreatedDate >= startDate &&
                      invoice.CreatedDate < endDate
                select new InvoiceDashboardDto
                {
                    Invoice = invoice,
                    Quotation = quotation
                };

            // Company Filter
            if (!isSuperAdmin)
            {
                quotationsQuery = quotationsQuery
                    .Where(x => x.CompanyId == loggedCompanyId);

                invoiceQuery =
                    from invoice in _context.QuotationInvoice
                    join quotation in _context.BillingQuotations
                        on invoice.QuotationReferenceNumber equals quotation.ReferenceNumber
                    where quotation.CompanyId == loggedCompanyId
                          && invoice.CreatedDate >= startDate
                          && invoice.CreatedDate < endDate
                    select new InvoiceDashboardDto
                    {
                        Invoice = invoice,
                        Quotation = quotation
                    };
            }

            // Summary
            var totalQuotations = await quotationsQuery.CountAsync();
            var totalInvoices = await invoiceQuery.CountAsync();

            var totalQuotationAmount = await quotationsQuery.SumAsync(x => x.TotalAmount);

            var totalInvoiceAmount = await invoiceQuery.SumAsync(x => x.Quotation.TotalAmount);

            // Quotation Status
            var quotationStatus = await quotationsQuery
                .GroupBy(x => x.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            // Invoice Status
            var invoiceStatus = await invoiceQuery
                .GroupBy(x => x.Invoice.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.Quotation.TotalAmount)
                })
                .ToListAsync();

            // Quotation Trend
            var quotationTrend = await quotationsQuery
                .GroupBy(x => x.CreatedDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Invoice Trend
            var invoiceTrend = await invoiceQuery
                .GroupBy(x => x.Invoice.CreatedDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.Quotation.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Ok(new
            {
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.AddDays(-1).ToString("yyyy-MM-dd"),

                Summary = new
                {
                    TotalQuotations = totalQuotations,
                    TotalQuotationAmount = totalQuotationAmount,

                    TotalInvoices = totalInvoices,
                    TotalInvoiceAmount = totalInvoiceAmount
                },

                QuotationStatus = quotationStatus,
                InvoiceStatus = invoiceStatus,

                QuotationTrend = quotationTrend,
                InvoiceTrend = invoiceTrend
            });
        }

        [HttpGet("details")]
        public async Task<IActionResult> GetDetails([FromQuery] string type, [FromQuery] string status, [FromQuery] DateTime? fromDate)
        {
            var loggedRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var loggedCompanyId = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;

            if (string.IsNullOrEmpty(type))
                return BadRequest("Type parameter is required.");

            bool isSuperAdmin = loggedRole == "Super Admin" || loggedRole == "SuperAdmin";

            // Frontend sends the start date (based on selected period)
            if (!fromDate.HasValue)
            {
                return BadRequest("From date is required.");
            }

            var startDate = fromDate.Value.Date;

            var endDate = DateTime.Today.AddDays(1);

            IQueryable<BillingQuotation> quotationsQuery = _context.BillingQuotations;
            var invoiceQuery = from invoice in _context.QuotationInvoice
                               join quotation in _context.BillingQuotations
                                   on invoice.QuotationReferenceNumber equals quotation.ReferenceNumber
                               select new
                               {
                                   invoice,
                                   quotation
                               };

            // ✅ ONLY ADDITION (APPLY DATE FILTER)
            quotationsQuery = quotationsQuery
                .Where(x => x.CreatedDate >= startDate && x.CreatedDate < endDate);

            invoiceQuery = invoiceQuery
                .Where(x => x.invoice.CreatedDate >= startDate &&
                            x.invoice.CreatedDate < endDate);

            if (!isSuperAdmin)
            {
                quotationsQuery = quotationsQuery.Where(x => x.CompanyId == loggedCompanyId);
                invoiceQuery = invoiceQuery.Where(x => x.quotation.CompanyId == loggedCompanyId);
            }

            if (type.Equals("Quotation", StringComparison.OrdinalIgnoreCase))
            {
                var query = _context.BillingQuotations.AsQueryable();

                if (status == "Total")
                {
                    var quotations = await quotationsQuery
                        .Select(quotation => new
                        {
                            Quotation = quotation.Id,
                            Refno = quotation.ReferenceNumber,
                            ReferenceNumber = quotation.ReferenceNumber,
                            //BillingEntity = quotation.BillingEntity,
                            ReceivingEntity = quotation.ReceivingEntity,
                            QuotationDate = quotation.QuotationDate,
                            QuotationStatus = quotation.Status,
                            Currency = quotation.Currency,
                            CompanyName = quotation.CompanyName,
                            VatNumber = quotation.VatNumber,
                            CompanyEmail = quotation.CompanyEmail,
                            CompanyAddress = quotation.CompanyAddress,
                            CompanyPhoneNumber = quotation.CompanyPhoneNumber,
                            BankAccountNumber = quotation.BankAccountNumber,
                            IFSCCode = quotation.IFSCCode,
                            BranchCode = quotation.BranchCode,
                            BranchAddress = quotation.BrannchAddress,
                            CustomerName = quotation.CustomerName,
                            CustomerEmail = quotation.CustomerEmail,
                            CustomerAddress = quotation.CustomerAddress,
                            CustomerPhone = quotation.CustomerPhone,
                            ComapanyLogo = quotation.Filename,
                            RegistrationNumber = quotation.RegistrationNumber,
                            QuotationType = quotation.QuotationType,
                            PaymentTerms = quotation.PaymentTerms,
                            Reason = quotation.RejectionReason,
                            SubTotal = quotation.SubTotal,
                            TotalAmount = quotation.TotalAmount,
                            BankName = quotation.BankName,
                            AccountHolderName = quotation.AccountHolderName,

                            Details = (from d in _context.BillingQuotationDetails
                                       join i in _context.InventoryItem
                                           on d.InventoryId equals i.Id.ToString()
                                       where d.ReferenceNumber == quotation.ReferenceNumber
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

                    return Ok(new
                    {
                        Type = "Quotation",
                        Count = quotations.Count,
                        Data = quotations
                    });
                }
                else if (status == "Created")
                {
                    var quotations = await quotationsQuery
                        .Where(x => x.Status == "Created" || x.Status == "Updated")
                        .Select(quotation => new
                        {
                            Quotation = quotation.Id,
                            Refno = quotation.ReferenceNumber,
                            ReferenceNumber = quotation.ReferenceNumber,
                            //BillingEntity = quotation.BillingEntity,
                            ReceivingEntity = quotation.ReceivingEntity,
                            QuotationDate = quotation.QuotationDate,
                            QuotationStatus = quotation.Status,
                            Currency = quotation.Currency,
                            CompanyName = quotation.CompanyName,
                            VatNumber = quotation.VatNumber,
                            CompanyEmail = quotation.CompanyEmail,
                            CompanyAddress = quotation.CompanyAddress,
                            CompanyPhoneNumber = quotation.CompanyPhoneNumber,
                            BankAccountNumber = quotation.BankAccountNumber,
                            IFSCCode = quotation.IFSCCode,
                            BranchCode = quotation.BranchCode,
                            BranchAddress = quotation.BrannchAddress,
                            CustomerName = quotation.CustomerName,
                            CustomerEmail = quotation.CustomerEmail,
                            CustomerAddress = quotation.CustomerAddress,
                            CustomerPhone = quotation.CustomerPhone,
                            ComapanyLogo = quotation.Filename,
                            RegistrationNumber = quotation.RegistrationNumber,
                            QuotationType = quotation.QuotationType,
                            PaymentTerms = quotation.PaymentTerms,
                            Reason = quotation.RejectionReason,
                            SubTotal = quotation.SubTotal,
                            TotalAmount = quotation.TotalAmount,
                            BankName = quotation.BankName,
                            AccountHolderName = quotation.AccountHolderName,

                            Details = (from d in _context.BillingQuotationDetails
                                       join i in _context.InventoryItem
                                           on d.InventoryId equals i.Id.ToString()
                                       where d.ReferenceNumber == quotation.ReferenceNumber
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

                    return Ok(new
                    {
                        Type = "Quotation",
                        Count = quotations.Count,
                        Data = quotations
                    });
                }
                else
                {
                    var quotations = await quotationsQuery
                        .Where(x => x.Status == status)
                        .Select(quotation => new
                        {
                            Quotation = quotation.Id,
                            Refno = quotation.ReferenceNumber,
                            ReferenceNumber = quotation.ReferenceNumber,
                            //BillingEntity = quotation.BillingEntity,
                            ReceivingEntity = quotation.ReceivingEntity,
                            QuotationDate = quotation.QuotationDate,
                            QuotationStatus = quotation.Status,
                            Currency = quotation.Currency,
                            CompanyName = quotation.CompanyName,
                            VatNumber = quotation.VatNumber,
                            CompanyEmail = quotation.CompanyEmail,
                            CompanyAddress = quotation.CompanyAddress,
                            CompanyPhoneNumber = quotation.CompanyPhoneNumber,
                            BankAccountNumber = quotation.BankAccountNumber,
                            IFSCCode = quotation.IFSCCode,
                            BranchCode = quotation.BranchCode,
                            BranchAddress = quotation.BrannchAddress,
                            CustomerName = quotation.CustomerName,
                            CustomerEmail = quotation.CustomerEmail,
                            CustomerAddress = quotation.CustomerAddress,
                            CustomerPhone = quotation.CustomerPhone,
                            ComapanyLogo = quotation.Filename,
                            RegistrationNumber = quotation.RegistrationNumber,
                            QuotationType = quotation.QuotationType,
                            PaymentTerms = quotation.PaymentTerms,
                            Reason = quotation.RejectionReason,
                            TotalAmount = quotation.TotalAmount,
                            BankName = quotation.BankName,
                            AccountHolderName = quotation.AccountHolderName,

                            Details = (from d in _context.BillingQuotationDetails
                                       join i in _context.InventoryItem
                                           on d.InventoryId equals i.Id.ToString()
                                       where d.ReferenceNumber == quotation.ReferenceNumber
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

                    return Ok(new
                    {
                        Type = "Quotation",
                        Count = quotations.Count,
                        Data = quotations
                    });
                }
            }
            else if (type.Equals("Invoice", StringComparison.OrdinalIgnoreCase))
            {
                if (status == "Total")
                {
                    var invoices = await invoiceQuery
                        .Select(x => new
                        {
                            QuotationId = x.quotation.Id,
                            Refno = x.quotation.ReferenceNumber,
                            ReferenceNumber = x.quotation.ReferenceNumber,
                            //BillingEntity = x.quotation.BillingEntity,
                            ReceivingEntity = x.quotation.ReceivingEntity,
                            QuotationDate = x.quotation.QuotationDate,
                            QuotationStatus = x.quotation.Status,
                            Currency = x.quotation.Currency,
                            CompanyName = x.quotation.CompanyName,
                            VatNumber = x.quotation.VatNumber,
                            CompanyEmail = x.quotation.CompanyEmail,
                            CompanyAddress = x.quotation.CompanyAddress,
                            CompanyPhoneNumber = x.quotation.CompanyPhoneNumber,
                            BankAccountNumber = x.quotation.BankAccountNumber,
                            IFSCCode = x.quotation.IFSCCode,
                            BranchCode = x.quotation.BranchCode,
                            BranchAddress = x.quotation.BrannchAddress,
                            CustomerName = x.quotation.CustomerName,
                            CustomerEmail = x.quotation.CustomerEmail,
                            CustomerAddress = x.quotation.CustomerAddress,
                            CustomerPhone = x.quotation.CustomerPhone,
                            ComapanyLogo = x.quotation.Filename,
                            RegistrationNumber = x.quotation.RegistrationNumber,
                            QuotationType = x.quotation.QuotationType,
                            PaymentTerms = x.quotation.PaymentTerms,
                            BankName = x.quotation.BankName,
                            AccountHolderName = x.quotation.AccountHolderName,

                            InvoiceRefno = x.invoice.InvoiceReferenceNumber,
                            InvoiceStatus = x.invoice.Status,
                            InvoiceDate = x.invoice.InvoiceDate,
                            Reason = x.invoice.RejectionReason,

                            SubTotal = x.quotation.SubTotal,
                            TotalAmount = x.quotation.TotalAmount,

                            Details = (from d in _context.BillingQuotationDetails
                                       join i in _context.InventoryItem
                                           on d.InventoryId equals i.Id.ToString()
                                       where d.ReferenceNumber == x.quotation.ReferenceNumber
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
                        })
                        .ToListAsync();

                    return Ok(new
                    {
                        Type = "Invoice",
                        Count = invoices.Count,
                        Data = invoices
                    });
                }
                else
                {
                    var invoices = await invoiceQuery
                        .Where(x => x.invoice.Status == status)
                        .Select(x => new
                        {
                            QuotationId = x.quotation.Id,
                            Refno = x.quotation.ReferenceNumber,
                            ReferenceNumber = x.quotation.ReferenceNumber,
                            //BillingEntity = x.quotation.BillingEntity,
                            ReceivingEntity = x.quotation.ReceivingEntity,
                            QuotationDate = x.quotation.QuotationDate,
                            QuotationStatus = x.quotation.Status,
                            Currency = x.quotation.Currency,
                            CompanyName = x.quotation.CompanyName,
                            VatNumber = x.quotation.VatNumber,
                            CompanyEmail = x.quotation.CompanyEmail,
                            CompanyAddress = x.quotation.CompanyAddress,
                            CompanyPhoneNumber = x.quotation.CompanyPhoneNumber,
                            BankAccountNumber = x.quotation.BankAccountNumber,
                            IFSCCode = x.quotation.IFSCCode,
                            BranchCode = x.quotation.BranchCode,
                            BranchAddress = x.quotation.BrannchAddress,
                            CustomerName = x.quotation.CustomerName,
                            CustomerEmail = x.quotation.CustomerEmail,
                            CustomerAddress = x.quotation.CustomerAddress,
                            CustomerPhone = x.quotation.CustomerPhone,
                            ComapanyLogo = x.quotation.Filename,
                            RegistrationNumber = x.quotation.RegistrationNumber,
                            QuotationType = x.quotation.QuotationType,
                            PaymentTerms = x.quotation.PaymentTerms,
                            BankName = x.quotation.BankName,
                            AccountHolderName = x.quotation.AccountHolderName,

                            InvoiceRefno = x.invoice.InvoiceReferenceNumber,
                            InvoiceStatus = x.invoice.Status,
                            InvoiceDate = x.invoice.InvoiceDate,
                            Reason = x.invoice.RejectionReason,

                            SubTotal = x.quotation.SubTotal,
                            TotalAmount = x.quotation.TotalAmount,

                            Details = (from d in _context.BillingQuotationDetails
                                       join i in _context.InventoryItem
                                           on d.InventoryId equals i.Id.ToString()
                                       where d.ReferenceNumber == x.quotation.ReferenceNumber
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
                        })
                        .ToListAsync();

                    return Ok(new
                    {
                        Type = "Invoice",
                        Count = invoices.Count,
                        Data = invoices
                    });
                }
            }

            return BadRequest("Invalid type parameter. Allowed values: Quotation, Invoice.");
        }

        [HttpGet("getQuotationorInvoicecountinreports")]
        public async Task<IActionResult> getQuotationorInvoicecountinreports()
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


        [HttpPost("getReortsData")]

        public async Task<IActionResult> getReortsData([FromQuery] string type)
        {
            var loggedRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var loggedEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            int? loggedCompanyId = null;
            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;

            if (int.TryParse(companyIdClaim, out var parsedCompanyId))
            {
                loggedCompanyId = parsedCompanyId;
            }
            if (string.IsNullOrEmpty(type))
                return BadRequest("Type parameter is required.");

            bool isSuperAdmin = loggedRole == "Super Admin" || loggedRole == "SuperAdmin";
            type = type.Trim();

            // =============================================================
            //  QUOTATIONS  (CreatedBy = int)
            // =============================================================
            if (type.Equals("Quotation", StringComparison.OrdinalIgnoreCase))
            {
                var quotationsQuery = _context.BillingQuotations.AsQueryable();

                if (!isSuperAdmin)
                {
                    quotationsQuery = quotationsQuery.Where(q => q.CompanyId == loggedCompanyId.ToString());
                }

                var quotations = await quotationsQuery
                    .Select(quotation => new
                    {
                        Quotation = quotation.Id,
                        refno = quotation.ReferenceNumber,
                        ReceivingEntity = quotation.ReceivingEntity,
                        quotationDate = quotation.QuotationDate,
                        QuotationStatus = quotation.Status,
                        currency = quotation.Currency,
                        companyName = quotation.CompanyName,
                        VatNumber = quotation.VatNumber,
                        CompanyEmail = quotation.CompanyEmail,
                        CompanyAddress = quotation.CompanyAddress,
                        CompanyPhoneNumber = quotation.CompanyPhoneNumber,
                        BankAccountNumber = quotation.BankAccountNumber,
                        IFSCCode = quotation.IFSCCode,
                        BranchCode = quotation.BranchCode,
                        BranchAddress = quotation.BrannchAddress,
                        customerName = quotation.CustomerName,
                        CustomerEmail = quotation.CustomerEmail,
                        CustomerAddress = quotation.CustomerAddress,
                        CustomerPhone = quotation.CustomerPhone,
                        ComapanyLogo = quotation.Filename,
                        RegistrationNumber = quotation.RegistrationNumber,
                        QuotationType = quotation.QuotationType,
                        PaymentTerms = quotation.PaymentTerms,
                        Reason = quotation.RejectionReason,
                        TotalAmount = quotation.TotalAmount,

                        Details = (from d in _context.BillingQuotationDetails
                                   join i in _context.InventoryItem on d.InventoryId equals i.Id.ToString()
                                   where d.ReferenceNumber == quotation.ReferenceNumber
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
                    })
                    .ToListAsync();

                return Ok(new
                {
                    type = "Quotation",
                    count = quotations.Count,
                    data = quotations
                });
            }

            // =============================================================
            //  INVOICES  (CreatedBy = int)
            // =============================================================
            else if (type.Equals("Invoice", StringComparison.OrdinalIgnoreCase))
            {
                var invoicesQuery = from invoice in _context.QuotationInvoice
                                    join quotation in _context.BillingQuotations
                                    on invoice.QuotationReferenceNumber equals quotation.ReferenceNumber
                                    select new { invoice, quotation };

                if (!isSuperAdmin)
                {
                    invoicesQuery = invoicesQuery.Where(x => x.quotation.CompanyId == loggedCompanyId.ToString());
                }

                var invoices = await invoicesQuery
                    .Select(x => new
                    {
                        QuotationId = x.quotation.Id,
                        InvoiceId = x.invoice.Id,
                        Refno = x.quotation.ReferenceNumber,
                        ReferenceNumber = x.quotation.ReferenceNumber,
                        //BillingEntity = x.quotation.BillingEntity,
                        ReceivingEntity = x.quotation.ReceivingEntity,
                        QuotationDate = x.quotation.QuotationDate,
                        QuotationStatus = x.quotation.Status,
                        Currency = x.quotation.Currency,
                        CompanyName = x.quotation.CompanyName,
                        VatNumber = x.quotation.VatNumber,
                        CompanyEmail = x.quotation.CompanyEmail,
                        CompanyAddress = x.quotation.CompanyAddress,
                        CompanyPhoneNumber = x.quotation.CompanyPhoneNumber,
                        BankAccountNumber = x.quotation.BankAccountNumber,
                        IFSCCode = x.quotation.IFSCCode,
                        BranchCode = x.quotation.BranchCode,
                        BranchAddress = x.quotation.BrannchAddress,
                        CustomerName = x.quotation.CustomerName,
                        CustomerEmail = x.quotation.CustomerEmail,
                        CustomerAddress = x.quotation.CustomerAddress,
                        CustomerPhone = x.quotation.CustomerPhone,
                        ComapanyLogo = x.quotation.Filename,
                        RegistrationNumber = x.quotation.RegistrationNumber,
                        QuotationType = x.quotation.QuotationType,
                        PaymentTerms = x.quotation.PaymentTerms,
                        InvoiceRefno = x.invoice.InvoiceReferenceNumber,
                        InvoiceStatus = x.invoice.Status,
                        InvoiceDate = x.invoice.InvoiceDate,
                        Reason = x.invoice.RejectionReason,
                        TotalAmount = x.quotation.TotalAmount,

                        Details = (from d in _context.BillingQuotationDetails
                                   join i in _context.InventoryItem on d.InventoryId equals i.Id.ToString()
                                   where d.ReferenceNumber == x.quotation.ReferenceNumber
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
                    })
                    .ToListAsync();

                return Ok(new
                {
                    type = "Invoice",
                    count = invoices.Count,
                    data = invoices
                });
            }

            // =============================================================
            //  CUSTOMERS (CreatedBy = string)
            // =============================================================
            else if (type.Equals("Customers", StringComparison.OrdinalIgnoreCase))
            {


                var customersQuery = from u in _context.Customers
                                     join c in _context.Companies on u.CompanyId equals c.Id
                                     select new { u, c };

                if (!isSuperAdmin)
                {
                    customersQuery = customersQuery.Where(x => x.u.CompanyId == loggedCompanyId);
                }

                var customers = await customersQuery
                    .Select(x => new
                    {
                        customerId = x.u.Id,
                        customerName = x.u.Name,
                        customerEmail = x.u.Email,
                        customerPhone = x.u.Mobile,
                        customerAddress = x.u.Address,
                        title = x.u.Title,
                        gender = x.u.Gender,
                        CustomerRefNo = x.u.CustomerRefNo,
                        companyName = x.c.CompanyName
                    })
                    .ToListAsync();

                return Ok(new
                {
                    type = "Customers",
                    count = customers.Count,
                    data = customers
                });
            }

            // =============================================================
            //  USERS (CreatedBy = string)
            // =============================================================
            else if (type.Equals("Users", StringComparison.OrdinalIgnoreCase))
            {
                var usersQuery = from u in _context.Users
                                 join r in _context.Roles on u.RoleId equals r.Id
                                 join c in _context.Companies on u.CompanyId equals c.Id
                                 select new { u, r, c };

                if (!isSuperAdmin)
                {
                    usersQuery = usersQuery.Where(x => x.u.CompanyId == loggedCompanyId);
                }

                var users = await usersQuery
                    .Select(x => new
                    {
                        userId = x.u.Id,
                        userName = x.u.UserName,
                        email = x.u.Email,
                        role = x.u.RoleId,
                        title = x.u.Title,
                        surName = x.u.SurName,
                        Name = x.u.Name,
                        mobile = x.u.Mobile,
                        gender = x.u.Gender,
                        address = x.u.Address,
                        roleName = x.r.RoleName,
                        companyName = x.c.CompanyName
                    })
                    .ToListAsync();

                return Ok(new
                {
                    type = "Users",
                    count = users.Count,
                    data = users
                });
            }

            // =============================================================
            //  PRODUCTS  (CreatedBy = int)
            // =============================================================
            else if (type.Equals("Products", StringComparison.OrdinalIgnoreCase))
            {
                var itemsQuery =
                    from item in _context.InventoryItem
                    join company in _context.Companies on item.CompanyId equals company.Id.ToString()
                    select new { item, company };

                if (!isSuperAdmin)
                {
                    itemsQuery = itemsQuery
                        .Where(i => i.item.CompanyId == loggedCompanyId.ToString());
                }

                var items = await itemsQuery
                    .Select(i => new
                    {
                        itemId = i.item.Id,
                        itemName = i.item.ItemName,
                        description = i.item.Description,
                        unitPrice = i.item.Price,
                        itemCode = i.item.ItemCode,
                        unitofMeasure = i.item.UnitOfMeasure,
                        taxRate = i.item.VAT,
                        Category = i.item.Category,
                        VatableStatus = i.item.VatableStatus,
                        companyName = i.company.CompanyName
                    })
                    .ToListAsync();

                return Ok(new
                {
                    type = "InventoryItems",
                    count = items.Count,
                    data = items
                });
            }

            return BadRequest("Invalid type parameter. Allowed: Quotation, Invoice, Customers, Users, Products.");
        }

        public class InvoiceDashboardDto
        {
            public QuotationInvoice Invoice { get; set; }
            public BillingQuotation Quotation { get; set; }
        }

        [HttpGet("outstandingamounts")]
        public async Task<IActionResult> GetOutstandingAmounts(string? companyName, string? Type)
        {
            var result = new List<OutstandingAmountDto>();

            using (var conn = _context.Database.GetDbConnection())
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SP_GetOutstandingAmounts";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(
                        new SqlParameter(
                            "@ReceivingEntity",
                            string.IsNullOrEmpty(companyName)
                                ? DBNull.Value
                                : companyName
                        )
                    );

                    cmd.Parameters.Add(
                        new SqlParameter(
                            "@Type",
                            string.IsNullOrEmpty(Type)
                                ? DBNull.Value
                                : Type
                        )
                    );

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new OutstandingAmountDto
                            {
                                CompanyName = reader["Company"]?.ToString(),

                                OutstandingAmount = reader["OutstandingAmount"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["OutstandingAmount"]),

                                NumberOfInvoices = reader["NumberOfInvoices"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(reader["NumberOfInvoices"]),

                                OverdueAmount = reader["OverdueAmount"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["OverdueAmount"]),

                                PaidAmount = reader["PaidAmount"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["PaidAmount"]),

                                ReferenceNumbers = reader["ReferenceNumbers"] == DBNull.Value
                                    ? null
                                    : reader["ReferenceNumbers"].ToString(),
                                Type = reader["Type"] == DBNull.Value? null: reader["Type"].ToString(),
                                ReceivableCompany = reader["Type"] == DBNull.Value? null: reader["ReceivingCompany"].ToString()


                            });
                        }
                    }
                }
            }

            return Ok(result);
        }
        [HttpGet("companyreceivables")]
        public async Task<IActionResult> Getcompanyreceivables(string companyName = null,string Type = null)
        {
            var result = new List<OutstandingInvoiceDto>();

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SP_GetCompanyReceivables";
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@ReceivingEntity", companyName));
                    command.Parameters.Add(new SqlParameter("@Type", string.IsNullOrEmpty(Type) ? DBNull.Value : Type));


                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new OutstandingInvoiceDto
                            {
                                Year = Convert.ToInt32(reader["Year"]),
                                MonthNo = Convert.ToInt32(reader["MonthNo"]),
                                Month = reader["Month"].ToString(),
                                CompanyOwedTo = reader["CompanyOwedTo"].ToString(),
                                OwedByCompany = reader["OwedByCompany"].ToString(),
                                NumberOfInvoices = reader["NumberOfInvoices"].ToString(),
                                TotalReceivableAmount = Convert.ToDecimal(reader["TotalReceivableAmount"]),
                                OverdueAmount = Convert.ToDecimal(reader["OverdueAmount"]),
                                PaidAmount = Convert.ToDecimal(reader["PaidAmount"]),
                                Type = reader["Type"].ToString(),


                            });
                        }
                    }
                }
            }

            return Ok(result);
        }


        [HttpGet("outstandingsummary")]
        public async Task<IActionResult> GetOutstandingSummary(string? companyName = null,string Type = null)
        {
            OutstandingSummaryDto result = null;

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SP_GetOutstandingSummary";
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@ReceivingEntity", string.IsNullOrEmpty(companyName) ? DBNull.Value : companyName));
                    command.Parameters.Add(new SqlParameter("@Type", string.IsNullOrEmpty(Type) ? DBNull.Value : Type));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result = new OutstandingSummaryDto
                            {
                                TotalOwedToCompany = reader["TotalOwedToCompany"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["TotalOwedToCompany"]),

                                NumberOfInvoices = reader["NumberOfInvoices"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(reader["NumberOfInvoices"]),

                                TotalOverdueAmount = reader["TotalOverdueAmount"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["TotalOverdueAmount"])
                            };
                        }
                    }
                }
            }

            return Ok(result);
        }

        [HttpGet("GetOutstandingInvoiceDetails")]
        public async Task<IActionResult> GetOutstandingInvoiceDetails([FromQuery] string companyName,[FromQuery] string? receivingEntity,[FromQuery] string? type)
        {
            List<QutationInvioceDto> invoices = new();
            List<QuotationInvoiceDetailDto> details = new();

            using SqlConnection con =
                new SqlConnection(_context.Database.GetConnectionString());

            using SqlCommand cmd =
                new SqlCommand("SP_GetOutstandingInvoiceDetails", con);

            cmd.CommandType = CommandType.StoredProcedure;

            // Company
            cmd.Parameters.AddWithValue(
                "@CompanyName",
                string.IsNullOrWhiteSpace(companyName)
                    ? DBNull.Value
                    : companyName
            );

            // Receiving Entity
            cmd.Parameters.AddWithValue(
                "@ReceivingEntity",
                string.IsNullOrWhiteSpace(receivingEntity)
                    ? DBNull.Value
                    : receivingEntity
            );

            // Type
            cmd.Parameters.AddWithValue(
                "@Type",
                string.IsNullOrWhiteSpace(type)
                    ? DBNull.Value
                    : type
            );

            await con.OpenAsync();

            using SqlDataReader reader =
                await cmd.ExecuteReaderAsync();

            // =========================================================
            // HEADER RESULT
            // =========================================================

            while (await reader.ReadAsync())
            {
                string? quotationReference =
                    reader["QuotationReferenceNumber"] == DBNull.Value
                        ? null
                        : reader["QuotationReferenceNumber"]?.ToString();

                string? invoiceReference =
                    reader["InvoiceReferenceNumber"] == DBNull.Value
                        ? null
                        : reader["InvoiceReferenceNumber"]?.ToString();

                // For quotation use quotation reference.
                // For invoice use invoice reference.
                // For BOTH use invoice reference if available,
                // otherwise quotation reference.
                string? referenceNumber =
                    !string.IsNullOrWhiteSpace(invoiceReference)
                        ? invoiceReference
                        : quotationReference;

                string? recordType = null;

                // Default/BOTH SP returns Type.
                try
                {
                    if (reader["Type"] != DBNull.Value)
                        recordType = reader["Type"]?.ToString();
                }
                catch (IndexOutOfRangeException)
                {
                    // Type column is not returned by Quotation/Invoice
                    // branches if not added to the SELECT.
                }

                // If SP was called with Type, we can also determine it here.
                if (string.IsNullOrWhiteSpace(recordType))
                {
                    if (string.Equals(type, "Quotation",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        recordType = "Quotation";
                    }
                    else if (string.Equals(type, "Invoice",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        recordType = "Invoice";
                    }
                    else
                    {
                        recordType = !string.IsNullOrWhiteSpace(invoiceReference)
                            ? "Invoice"
                            : "Quotation";
                    }
                }

                invoices.Add(new QutationInvioceDto
                {
                    QuotationId =
                        reader["QuotationId"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["QuotationId"]),

                    QuotationDate =
                        reader["QuotationDate"] == DBNull.Value
                            ? (DateTime?)null
                            : Convert.ToDateTime(reader["QuotationDate"]),

                    QuotationReferenceNumber =
                        quotationReference,

                    ReferenceNumber =
                        referenceNumber,

                    QuotationStatus =
                        reader["QuotationStatus"] == DBNull.Value
                            ? null
                            : reader["QuotationStatus"]?.ToString(),

                    InvoiceId =
                        reader["InvoiceId"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["InvoiceId"]),

                    InvoiceReferenceNumber =
                        invoiceReference,

                    InvoiceStatus =
                        reader["InvoiceStatus"] == DBNull.Value
                            ? null
                            : reader["InvoiceStatus"]?.ToString(),

                    InvoiceDate =
                        reader["InvoiceDate"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(reader["InvoiceDate"]),

                    DueDate =
                        reader["DueDate"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(reader["DueDate"]),

                    ReceivingEntity =
                        reader["ReceivingEntity"] == DBNull.Value
                            ? null
                            : reader["ReceivingEntity"]?.ToString(),

                    Currency =
                        reader["Currency"] == DBNull.Value
                            ? null
                            : reader["Currency"]?.ToString(),

                    CompanyName =
                        reader["CompanyName"] == DBNull.Value
                            ? null
                            : reader["CompanyName"]?.ToString(),

                    VatNumber =
                        reader["VatNumber"] == DBNull.Value
                            ? null
                            : reader["VatNumber"]?.ToString(),

                    CompanyEmail =
                        reader["CompanyEmail"] == DBNull.Value
                            ? null
                            : reader["CompanyEmail"]?.ToString(),

                    CompanyAddress =
                        reader["CompanyAddress"] == DBNull.Value
                            ? null
                            : reader["CompanyAddress"]?.ToString(),

                    CompanyPhoneNumber =
                        reader["CompanyPhoneNumber"] == DBNull.Value
                            ? null
                            : reader["CompanyPhoneNumber"]?.ToString(),

                    BankAccountNumber =
                        reader["BankAccountNumber"] == DBNull.Value
                            ? null
                            : reader["BankAccountNumber"]?.ToString(),

                    CompanyWebsite =
                        reader["CompanyWebsite"] == DBNull.Value
                            ? null
                            : reader["CompanyWebsite"]?.ToString(),

                    IFSCCode =
                        reader["IFSCCode"] == DBNull.Value
                            ? null
                            : reader["IFSCCode"]?.ToString(),

                    BranchCode =
                        reader["BranchCode"] == DBNull.Value
                            ? null
                            : reader["BranchCode"]?.ToString(),

                    BranchAddress =
                        reader["BranchAddress"] == DBNull.Value
                            ? null
                            : reader["BranchAddress"]?.ToString(),

                    CustomerName =
                        reader["CustomerName"] == DBNull.Value
                            ? null
                            : reader["CustomerName"]?.ToString(),

                    CustomerEmail =
                        reader["CustomerEmail"] == DBNull.Value
                            ? null
                            : reader["CustomerEmail"]?.ToString(),

                    CustomerAddress =
                        reader["CustomerAddress"] == DBNull.Value
                            ? null
                            : reader["CustomerAddress"]?.ToString(),

                    CustomerPhone =
                        reader["CustomerPhone"] == DBNull.Value
                            ? null
                            : reader["CustomerPhone"]?.ToString(),

                    CompanyLogo =
                        reader["CompanyLogo"] == DBNull.Value
                            ? null
                            : reader["CompanyLogo"]?.ToString(),

                    RegistrationNumber =
                        reader["RegistrationNumber"] == DBNull.Value
                            ? null
                            : reader["RegistrationNumber"]?.ToString(),

                    PaymentTerms =
                        reader["PaymentTerms"] == DBNull.Value
                            ? null
                            : reader["PaymentTerms"]?.ToString(),

                    QuotationType =
                        reader["QuotationType"] == DBNull.Value
                            ? null
                            : reader["QuotationType"]?.ToString(),

                    BankName =
                        reader["BankName"] == DBNull.Value
                            ? null
                            : reader["BankName"]?.ToString(),

                    AccountHolderName =
                        reader["AccountHolderName"] == DBNull.Value
                            ? null
                            : reader["AccountHolderName"]?.ToString(),

                    Reason =
                        reader["Reason"] == DBNull.Value
                            ? null
                            : reader["Reason"]?.ToString(),

                    TotalAmount =
                        reader["TotalAmount"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["TotalAmount"]),

                    SubTotal =
                        reader["SubTotal"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["SubTotal"]),

                    TaxAmount =
                        reader["TaxAmount"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["TaxAmount"]),

                    // Add this property to DTO
                    Type = recordType,

                    Detail = new List<QuotationInvoiceDetailDto>()
                });
            }


            // =========================================================
            // DETAILS RESULT
            // =========================================================

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    details.Add(new QuotationInvoiceDetailDto
                    {
                        ReferenceNumber =
                            reader["ReferenceNumber"] == DBNull.Value
                                ? null
                                : reader["ReferenceNumber"]?.ToString(),

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
                            reader["Description"] == DBNull.Value
                                ? null
                                : reader["Description"]?.ToString(),

                        Category =
                            reader["Category"] == DBNull.Value
                                ? null
                                : reader["Category"]?.ToString(),

                        ItemCode =
                            reader["ItemCode"] == DBNull.Value
                                ? null
                                : reader["ItemCode"]?.ToString(),

                        ItemName =
                            reader["ItemName"] == DBNull.Value
                                ? null
                                : reader["ItemName"]?.ToString(),

                        Discount =
                            reader["Discount"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader["Discount"]),

                        DiscountType =
                            reader["DiscountType"] == DBNull.Value
                                ? null
                                : reader["DiscountType"]?.ToString(),

                        Tax =
                            reader["TaxAmount"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader["TaxAmount"])
                    });
                }
            }


            // =========================================================
            // MAP DETAILS TO HEADER
            // =========================================================

            foreach (var invoice in invoices)
            {
                if (string.IsNullOrWhiteSpace(invoice.ReferenceNumber))
                {
                    invoice.Detail = new List<QuotationInvoiceDetailDto>();
                    continue;
                }

                invoice.Detail = details
                    .Where(x =>
                        string.Equals(
                            x.ReferenceNumber,
                            invoice.ReferenceNumber,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .ToList();
            }


            // =========================================================
            // RESPONSE
            // =========================================================

            return Ok(new
            {
                list = invoices.Select(x => new
                {
                    header = x,
                    detail = x.Detail
                })
            });
        }
    }
}
