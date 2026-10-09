using InterCompanyBillingSulution.Models;
using InterCompanyBillingSulution.Models.DTO;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.DataAcess.Data
{
    public  class InterCompanyBillingDbContext : IdentityDbContext
    {
     public InterCompanyBillingDbContext(DbContextOptions options) : base(options)
    {

    }
        public DbSet<Users> Users { get; set; }
        public DbSet<Companies> Companies { get; set; }
        public DbSet<VAT> VAT { get; set; }
        public DbSet<Status> Status { get; set; }
        public DbSet<BillingQuotation> BillingQuotations { get; set; }
        public DbSet<BillingQuotationDetails> BillingQuotationDetails { get; set; }
        public DbSet<InventoryItem> InventoryItem { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Screens> Screens { get; set; }
        public DbSet<ScreenMappingWithRole> ScreenMappingWithRoles { get; set; }
        public DbSet<NotificationsControlTable> NotificationsControlTable { get; set; }
        public DbSet<QuotationInvoice> QuotationInvoice { get; set; }
        public DbSet<Customers> Customers { get; set; }
        public DbSet<QuotationAudit> QuotationAudit { get; set; }
        public DbSet<QuotationDetailsAudit> QuotationDetailsAudit { get; set; }
        public DbSet<LoginHistory> LoginHistory { get; set; }
        public DbSet<InventoryItemAudit> InventoryItemAudit { get; set; }
        public DbSet<CompaniesAudit> CompaniesAudit { get; set; }
        public DbSet<CustomersAudit> CustomersAudit { get; set; }
        public DbSet<QuotationInvoiceAudit> QuotationInvoiceAudit { get; set; }
        public DbSet<RolesAudit> RolesAudit { get; set; }
        public DbSet<UsersAudit> UsersAudit { get; set; }
        public DbSet<HelpControlTable> HelpControlTable { get; set; }
        public DbSet<MeterType> MeterType { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CreditNoteHeader> CreditNoteHeader { get; set; }
        public DbSet<CreditNoteDetails> CreditNoteDetails { get; set; }
        public DbSet<QuotationInvoiceDetails> QuotationInvoiceDetails { get; set; }
        public DbSet<PaymentUploads> PaymentUploads { get; set; }
        public DbSet<UserListDto> UserListDtos { get; set; }
        public DbSet<HistoricalBacklog> historicalBacklog { get; set; }
        public DbSet<QuotationType> QuotationType { get; set; }
        public DbSet<QuotationPayments> QuotationPayments { get; set; }

        public DbSet<UserMappedCompanies> UserMappedCompanies { get; set; }


        //protected override void OnModelCreating(ModelBuilder builder)
        //{
        //base.OnModelCreating(builder);
        //    builder.Entity<BillingQuotation>()
        //.HasIndex(b => b.ReferenceNumber)
        //.IsUnique();
        //}

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<BillingQuotation>()
                .HasIndex(b => b.ReferenceNumber)
                .IsUnique();

            builder.Entity<UserListDto>()
                .HasNoKey()
                .ToView(null);

        }

    }
}
