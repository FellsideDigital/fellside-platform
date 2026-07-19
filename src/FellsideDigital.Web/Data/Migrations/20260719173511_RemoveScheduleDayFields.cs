using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FellsideDigital.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveScheduleDayFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayOfMonth",
                table: "RecurringInvoiceSchedules");

            migrationBuilder.DropColumn(
                name: "DueDays",
                table: "RecurringInvoiceSchedules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DayOfMonth",
                table: "RecurringInvoiceSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DueDays",
                table: "RecurringInvoiceSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
