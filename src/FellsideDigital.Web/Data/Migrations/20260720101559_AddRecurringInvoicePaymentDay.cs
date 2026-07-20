using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FellsideDigital.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringInvoicePaymentDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing schedules were all billed on the global payment day; default them to the
            // 1st (the app's global default). New rows always set the value explicitly.
            migrationBuilder.AddColumn<int>(
                name: "PaymentDayOfMonth",
                table: "RecurringInvoiceSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentDayOfMonth",
                table: "RecurringInvoiceSchedules");
        }
    }
}
