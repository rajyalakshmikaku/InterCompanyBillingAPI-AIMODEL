using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterCompanyBillingSulution.DataAcess.Migrations
{
    /// <inheritdoc />
    public partial class addingHistoricalBacklog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "historicalBacklog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    From_Company_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    From_Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    From_Phone_Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    From_Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    From_VAT_Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    From_Registration_Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    To_Company_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    To_Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    To_Phone_Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    To_Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    To_VAT_Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    To_Registration_Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mobile_Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Item_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit_Rate_Per_Item = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tax_Percentage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reference_Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quotation_Date = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuotationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bank_Account_Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bank_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Branch_Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Branch_Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bank_Account_Holder_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subtotal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tax_Amount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Total_Amount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Invoice_Reference_Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Invoice_Date = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Payment_Terms = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historicalBacklog", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historicalBacklog");
        }
    }
}
