using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatHelper.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionKeyAndDeviceInfoToEmailLoginCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "EmailLoginCodes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Platform",
                table: "EmailLoginCodes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SessionKey",
                table: "EmailLoginCodes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "EmailLoginCodes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "EmailLoginCodes");

            migrationBuilder.DropColumn(
                name: "Platform",
                table: "EmailLoginCodes");

            migrationBuilder.DropColumn(
                name: "SessionKey",
                table: "EmailLoginCodes");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "EmailLoginCodes");
        }
    }
}
