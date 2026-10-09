using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterCompanyBillingSulution.DataAcess.Migrations
{
    /// <inheritdoc />
    public partial class addingStatuscolumnsinQutationtables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "QuotationAudit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "QuotationAudit",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnpaidStatus",
                table: "QuotationAudit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnpaidStatus",
                table: "BillingQuotations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "QuotationAudit");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "QuotationAudit");

            migrationBuilder.DropColumn(
                name: "UnpaidStatus",
                table: "QuotationAudit");

            migrationBuilder.DropColumn(
                name: "UnpaidStatus",
                table: "BillingQuotations");

            
        }
    }
}
