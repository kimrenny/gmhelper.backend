using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatHelper.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddRememberToEmailLoginCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Remember",
                table: "EmailLoginCodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Remember",
                table: "EmailLoginCodes");
        }
    }
}
