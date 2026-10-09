using System;
using System.Security.Claims;
using System.Threading.Tasks;
using InterCompanyBillingSulution.DataAcess.Data;
using InterCompanyBillingSulution.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InterCompanyBillingSulution_Core
{

    public class AuditService
    {
        private readonly InterCompanyBillingDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(InterCompanyBillingDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;

        }
        public async Task AddUserAuditAsync(Users user, string action)
        {
            var userClaims = _httpContextAccessor.HttpContext?.User;
            var loggedEmail = userClaims?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;


            string ipAddress = "0.0.0.0"; // default non-null

            try
            {
                ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            }
            catch
            {
                ipAddress = "0.0.0.0";
            }

            var roleName = await _context.Roles
                .Where(r => r.Id == user.RoleId)
                .Select(r => r.RoleName)
                .FirstOrDefaultAsync();

            var audit = new UsersAudit
            {
                UserId = user.Id,
                RoleId = user.RoleId,
                UserName = user.UserName,
                Password = user.Password,
                Status = user.Status,
                CreatedBy = user.CreatedBy,
                CreatedDate = user.CreatedDate,
                ModifiedBy = user.ModifiedBy,
                ModifiedDate = user.ModifiedDate,
                Name = user.Name,
                SurName = user.SurName,
                Email = user.Email,
                CompanyName = user.CompanyId,
                Title = user.Title,
                Gender = user.Gender,
                Activation = user.Activation,
                CustomerAccountNo = user.CustomerAccountNo,
                Address = user.Address,
                Mobile = user.Mobile,
                ActionBy = user.ModifiedBy,
                Action = action,
                ActionDate = DateTime.Now,
                IPaddressOfSystem = ipAddress   
            };

            _context.UsersAudit.Add(audit);
            await _context.SaveChangesAsync();
        }
        public async Task AddCustomerAuditAsync(Customers customer, string action)
        {

            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var userClaims = _httpContextAccessor.HttpContext?.User;
            var loggedEmail = userClaims?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;


            string ipAddress = "N/A";
            try
            {
                ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "N/A";
            }
            catch
            {
                ipAddress = "N/A";
            }

            var audit = new CustomersAudit
            {
                CustomerId = customer.Id,
                Title = customer.Title,
                Name = customer.Name,
                SurName = customer.SurName,
                Email = customer.Email,
                CompanyId = customer.CompanyId,
                Gender = customer.Gender,
                CustomerRefNo = customer.CustomerRefNo,
                VATNumber = customer.VATNumber,
                Address = customer.Address,
                Mobile = customer.Mobile,
                UserName = customer.UserName,
                Status = customer.Status,
                CreatedBy = customer.CreatedBy,
                CreatedDate = customer.CreatedDate,
                ModifiedBy = customer.ModifiedBy,
                ModifiedDate = customer.ModifiedDate,
                ActionBy = loggedEmail,
                ActionDate = DateTime.Now,
                Action = action,
                IPaddressOfSystem = ipAddress
            };

            _context.CustomersAudit.Add(audit);
            await _context.SaveChangesAsync();
        }
        public async Task AddQuotationAudit(BillingQuotation quotation, string action)
        {
            if (quotation == null)
                throw new ArgumentNullException(nameof(quotation));
            var userClaims = _httpContextAccessor.HttpContext?.User;
            var loggedEmail = userClaims?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;


            string ipAddress = "N/A";
            try
            {
                ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "N/A";
            }
            catch
            {
                ipAddress = "N/A";
            }
            var quotationAudit = new QuotationAudit
            {
                QuotationId = quotation.Id,
                ReferenceNumber = quotation.ReferenceNumber,
                QuotationDate = quotation.QuotationDate,
                ReceivingEntity = quotation.ReceivingEntity,
                Currency = quotation.Currency,
                QuotationType = quotation.QuotationType,
                CompanyName = quotation.CompanyName,
                VatNumber = quotation.VatNumber,
                RegistrationNumber = quotation.RegistrationNumber,
                CompanyAddress = quotation.CompanyAddress,
                CompanyWebsite = quotation.CompanyWebsite,
                CompanyEmail = quotation.CompanyEmail,
                CompanyPhoneNumber = quotation.CompanyPhoneNumber,
                CustomerName = quotation.CustomerName,
                CustomerAddress = quotation.CustomerAddress,
                CustomerEmail = quotation.CustomerEmail,
                CustomerPhone = quotation.CustomerPhone,
                BankAccountNumber = quotation.BankAccountNumber,
                BrannchAddress = quotation.BrannchAddress,
                BranchCode = quotation.BranchCode,
                IFSCCode = quotation.IFSCCode,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedBy = quotation.CreatedBy,
                ModifiedBy = quotation.ModifiedBy,
                Status = quotation.Status,
                Filename = quotation.Filename,
                ActionBy = loggedEmail,
                ActionDate = DateTime.Now,
                Action = action,
                IPaddressOfSystem = ipAddress,
                AccountHolderName = quotation.AccountHolderName,
                CustomerRefno = quotation.CustomerRefno,
                RejectionReason = quotation.RejectionReason,
                RejectedDate = quotation.RejectedDate,
                CompanyId = quotation.CompanyId,
                PaymentTerms = quotation.PaymentTerms,
                BankName = quotation.BankName,
                DueDate = quotation.DueDate,
                TotalAmount =quotation.TotalAmount,
                SubTotal = quotation.SubTotal,
                TaxAmount = quotation.TaxAmount,
                
            };

            _context.QuotationAudit.Add(quotationAudit);
            await _context.SaveChangesAsync();
        }
        public async Task AddInventoryAudit(InventoryItem customer, string action)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));
            var userClaims = _httpContextAccessor.HttpContext?.User;
            var loggedEmail = userClaims?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;



            string ipAddress = "N/A";
            try
            {
                ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "N/A";
            }
            catch
            {
                ipAddress = "N/A";
            }

            var audit = new InventoryItemAudit
            {
                ItemId = customer.Id,
                ItemName = customer.ItemName,
                Description = customer.Description,
                Price = customer.Price,
                CompanyId = customer.CompanyId,
                ItemCode = customer.ItemCode,
                Category = customer.Category,
                UnitOfMeasure = customer.UnitOfMeasure,
                VAT = customer.VAT,
                Status = customer.Status,
                CreatedBy = customer.CreatedBy,
                CreatedDate = customer.CreatedDate,
                ModifiedBy = customer.ModifiedBy,
                ModifiedDate = customer.ModifiedDate,
                VatableStatus = customer.VatableStatus,
                ActionBy = loggedEmail,
                ActionDate = DateTime.Now,
                Action = action,
                IPaddressOfSystem = ipAddress
            };

            _context.InventoryItemAudit.Add(audit);
            await _context.SaveChangesAsync();
        }
        public async Task AddRolesAuditAsync(Roles roles, string action)
        {
            var userClaims = _httpContextAccessor.HttpContext?.User;
            var loggedEmail = userClaims?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;


            if (roles == null)
                throw new ArgumentNullException(nameof(roles));

            string ipAddress = "N/A";
            try
            {
                ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "N/A";
            }
            catch
            {
                ipAddress = "N/A";
            }
            var audit = new RolesAudit
            {
                RolesId = roles.Id,
                RoleName = roles.RoleName,
                RoleDescription = roles.RoleDescription,
                Status = roles.Status,
                CreatedBy = roles.CreatedBy ?? loggedEmail ?? "",
                CreatedDate = roles.CreatedDate,
                ModifiedBy = roles.ModifiedBy ?? loggedEmail ?? "",
                ModifiedDate = roles.ModifiedDate,
                ActionBy = loggedEmail,
                ActionDate = DateTime.Now,
                Action = action,
                IPaddressOfSystem = ipAddress,
            };

            _context.RolesAudit.Add(audit);
            await _context.SaveChangesAsync();
        }
        public async Task AddCompaniesAuditAsync(Companies companies, string action)
        {
            var userClaims = _httpContextAccessor.HttpContext?.User;
            var loggedEmail = userClaims?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;



            if (companies == null)
                throw new ArgumentNullException(nameof(companies));

            string ipAddress = "N/A";
            try
            {
                ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "N/A";
            }
            catch
            {
                ipAddress = "N/A";
            }

            var audit = new CompaniesAudit
            {
                CompanyId = companies.Id,
                CompanyName = companies.CompanyName,
                VatNumber = companies.VatNumber,
                CompanyPhoneNumber = companies.CompanyPhoneNumber,
                CompanyAddress = companies.CompanyAddress,
                CompanyEmail = companies.CompanyEmail,
                CompanyWebsite = companies.CompanyWebsite,
                RegistrationNumber = companies.RegistrationNumber,
                BranchCode = companies.BranchCode,
                BranchAddress = companies.BranchAddress,
                BankAccountNumber = companies.BankAccountNumber,
                IFSCCode = companies.IFSCCode,
                AccountHolderName = companies.AccountHolderName,
                CompanyLogo = companies.CompanyLogo,
                FilePath = companies.FilePath,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                Status = companies.Status,
                CreatedBy = companies.CreatedBy,
                ModifiedBy = companies.ModifiedBy,
                ActionBy = loggedEmail,
                ActionDate = DateTime.Now,
                Action = action,
                IPaddressOfSystem = ipAddress,
                BankName = companies.BankName,
            };

            _context.CompaniesAudit.Add(audit);
            await _context.SaveChangesAsync();
        }
        public async Task AddInvoiceAudit(QuotationInvoice Invoice, string action)
        {
            var userClaims = _httpContextAccessor.HttpContext?.User;
            var loggedEmail = userClaims?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;


            if (Invoice == null)
                throw new ArgumentNullException(nameof(Invoice));

            string ipAddress = "N/A";
            try
            {
                ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "N/A";
            }
            catch
            {
                ipAddress = "N/A";
            }
            var audit = new QuotationInvoiceAudit
            {
                InvoiceId = Invoice.Id,
                QuotationReferenceNumber = Invoice.QuotationReferenceNumber,
                InvoiceReferenceNumber = Invoice.InvoiceReferenceNumber,
                InvoiceDate = Invoice.InvoiceDate,
                Status = Invoice.Status,
                RejectionReason = Invoice.RejectionReason,
                ReceivingEntity = Invoice.ReceivingEntity,
                Currency = Invoice.Currency,
                QuotationType = Invoice.QuotationType,
                CompanyName = Invoice.CompanyName,
                VatNumber = Invoice.VatNumber,
                RegistrationNumber = Invoice.RegistrationNumber,
                CompanyAddress = Invoice.CompanyAddress,
                CompanyWebsite = Invoice.CompanyWebsite,
                CompanyEmail = Invoice.CompanyEmail,
                CompanyPhoneNumber = Invoice.CompanyPhoneNumber,
                CustomerName = Invoice.CustomerName,
                CustomerAddress = Invoice.CustomerAddress,
                CustomerEmail = Invoice.CustomerEmail,
                CustomerPhone = Invoice.CustomerPhone,
                BranchAddress = Invoice.BranchAddress,
                BankAccountNumber = Invoice.BankAccountNumber,
                BranchCode = Invoice.BranchCode,
                IFSCCode = Invoice.IFSCCode,
                Filename = Invoice.Filename,
                AccountHolderName = Invoice.AccountHolderName,
                CustomerRefno = Invoice.CustomerRefno,
                RejectedDate = Invoice.RejectedDate,
                CompanyId = Invoice.CompanyId,
                PaymentTerms = Invoice.PaymentTerms,
                BankName = Invoice.BankName,
                TotalAmount = Invoice.TotalAmount,
                SubTotal = Invoice.SubTotal,
                TaxAmount = Invoice.TaxAmount,

                CreatedDate = Invoice.CreatedDate,
                ModifiedDate = Invoice.ModifiedDate,
                CreatedBy = Invoice.CreatedBy,
                ModifiedBy = Invoice.ModifiedBy,
                Action = action,
                ActionDate = DateTime.Now,
                IPaddressOfSystem = ipAddress,
                ActionBy = loggedEmail,

            };

            _context.QuotationInvoiceAudit.Add(audit);
            await _context.SaveChangesAsync();
        }
        public async Task AddQuotationAuditwithoutsession(BillingQuotation quotation, string action)
        {
            if (quotation == null)
                throw new ArgumentNullException(nameof(quotation));

            string ipAddress = "N/A";
            try
            {
                ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "N/A";
            }
            catch
            {
                ipAddress = "N/A";
            }
            var quotationAudit = new QuotationAudit
            {
                QuotationId = quotation.Id,
                ReferenceNumber = quotation.ReferenceNumber,
                QuotationDate = quotation.QuotationDate,
                //BillingEntity = quotation.BillingEntity,
                ReceivingEntity = quotation.ReceivingEntity,
                Currency = quotation.Currency,
                //NoteComments = quotation.NoteComments,
                //ProjectCostCenter = quotation.ProjectCostCenter,
                QuotationType = quotation.QuotationType,
                CompanyName = quotation.CompanyName,
                VatNumber = quotation.VatNumber,
                RegistrationNumber = quotation.RegistrationNumber,
                CompanyAddress = quotation.CompanyAddress,
                CompanyWebsite = quotation.CompanyWebsite,
                CompanyEmail = quotation.CompanyEmail,
                CompanyPhoneNumber = quotation.CompanyPhoneNumber,
                CustomerName = quotation.CustomerName,
                CustomerAddress = quotation.CustomerAddress,
                CustomerEmail = quotation.CustomerEmail,
                CustomerPhone = quotation.CustomerPhone,
                BankAccountNumber = quotation.BankAccountNumber,
                BrannchAddress = quotation.BrannchAddress,
                BranchCode = quotation.BranchCode,
                IFSCCode = quotation.IFSCCode,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedBy = quotation.CreatedBy,
                ModifiedBy = quotation.ModifiedBy,
                Status = quotation.Status,
                Filename = quotation.Filename,
                ActionBy = quotation.CustomerEmail,
                CompanyId = quotation.CompanyId,
                AccountHolderName = quotation.AccountHolderName,
                PaymentTerms = quotation.PaymentTerms,
                RejectionReason = quotation.RejectionReason,
                RejectedDate = quotation.RejectedDate,
                CustomerRefno = quotation.CustomerRefno,
                BankName = quotation.BankName,
                ActionDate = DateTime.Now,
                Action = action,
                IPaddressOfSystem = ipAddress,
                DueDate = quotation.DueDate,
            };

            _context.QuotationAudit.Add(quotationAudit);
            await _context.SaveChangesAsync();
        }


    }

}


