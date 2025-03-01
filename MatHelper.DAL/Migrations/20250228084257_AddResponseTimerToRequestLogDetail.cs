using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatHelper.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseTimerToRequestLogDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "StatusCode",
                table: "RequestLogDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ElapsedTime",
                table: "RequestLogDetails",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "EndTime",
                table: "RequestLogDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartTime",
                table: "RequestLogDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElapsedTime",
                table: "RequestLogDetails");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "RequestLogDetails");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "RequestLogDetails");

            migrationBuilder.AlterColumn<string>(
                name: "StatusCode",
                table: "RequestLogDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
