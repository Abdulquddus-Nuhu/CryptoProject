using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoProject.Migrations
{
    /// <inheritdoc />
    public partial class modifyCrypto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WalletType",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WalletType",
                table: "Transactions");
        }
    }
}
