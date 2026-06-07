using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaverickBank.Migrations
{
    /// <inheritdoc />
    public partial class LoantypeFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MinimunTenureMonths",
                table: "LoanTypes",
                newName: "MinimumTenureMonths");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MinimumTenureMonths",
                table: "LoanTypes",
                newName: "MinimunTenureMonths");
        }
    }
}
