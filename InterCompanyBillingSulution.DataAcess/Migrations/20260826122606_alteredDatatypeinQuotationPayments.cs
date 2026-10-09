using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterCompanyBillingSulution.DataAcess.Migrations
{
    /// <inheritdoc />
    public partial class alteredDatatypeinQuotationPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.AlterColumn<string>(
                name: "QuotationRefNo",
                table: "QuotationPayments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "QuotationRefNo",
                table: "QuotationPayments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

           
        }
    }
}
