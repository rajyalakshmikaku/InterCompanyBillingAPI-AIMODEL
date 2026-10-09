using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterCompanyBillingSulution.DataAcess.Migrations
{
    /// <inheritdoc />
    public partial class ReasonaddedinUserstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
          
        }
    }
}
