using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoProject.Migrations
{
    /// <inheritdoc />
    public partial class modifyTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReceiverWalletAddress",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Details",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ReceiverWalletAddress",
                table: "Transactions");
        }
    }
}
