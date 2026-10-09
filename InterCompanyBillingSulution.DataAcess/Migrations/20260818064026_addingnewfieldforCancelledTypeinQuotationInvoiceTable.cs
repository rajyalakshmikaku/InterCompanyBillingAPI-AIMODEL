using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterCompanyBillingSulution.DataAcess.Migrations
{
    /// <inheritdoc />
    public partial class addingnewfieldforCancelledTypeinQuotationInvoiceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                table: "QuotationInvoice",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "QuotationInvoice");
        }
    }
}
