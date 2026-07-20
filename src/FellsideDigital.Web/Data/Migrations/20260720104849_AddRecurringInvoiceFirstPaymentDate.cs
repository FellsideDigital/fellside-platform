using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FellsideDigital.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringInvoiceFirstPaymentDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstPaymentDate",
                table: "RecurringInvoiceSchedules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Backfill existing schedules to their platform setup date. Admins can back-date
            // further for retainers that began before the platform to get the true total.
            migrationBuilder.Sql(
                "UPDATE \"RecurringInvoiceSchedules\" SET \"FirstPaymentDate\" = \"CreatedAt\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstPaymentDate",
                table: "RecurringInvoiceSchedules");
        }
    }
}
