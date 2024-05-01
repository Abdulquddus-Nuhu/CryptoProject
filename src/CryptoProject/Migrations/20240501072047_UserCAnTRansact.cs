using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoProject.Migrations
{
    /// <inheritdoc />
    public partial class UserCAnTRansact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanTransact",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanTransact",
                table: "AspNetUsers");
        }
    }
}
