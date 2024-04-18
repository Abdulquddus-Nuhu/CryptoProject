using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoProject.Migrations
{
    /// <inheritdoc />
    public partial class userPasword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Wallets",
                schema: "public",
                newName: "Wallets");

            migrationBuilder.RenameTable(
                name: "USDAccounts",
                schema: "public",
                newName: "USDAccounts");

            migrationBuilder.RenameTable(
                name: "Transactions",
                schema: "public",
                newName: "Transactions");

            migrationBuilder.RenameTable(
                name: "LedgerAccounts",
                schema: "public",
                newName: "LedgerAccounts");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                schema: "public",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                schema: "public",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                schema: "public",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                schema: "public",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                schema: "public",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                schema: "public",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                schema: "public",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "ActivityLogs",
                schema: "public",
                newName: "ActivityLogs");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "Wallets",
                newName: "Wallets",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "USDAccounts",
                newName: "USDAccounts",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "Transactions",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "LedgerAccounts",
                newName: "LedgerAccounts",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "AspNetUserTokens",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "AspNetUsers",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "AspNetUserRoles",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "AspNetUserLogins",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "AspNetUserClaims",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "AspNetRoles",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "AspNetRoleClaims",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "ActivityLogs",
                newName: "ActivityLogs",
                newSchema: "public");
        }
    }
}
