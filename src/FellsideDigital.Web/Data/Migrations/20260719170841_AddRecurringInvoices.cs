using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FellsideDigital.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReminderStage",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduleId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecurringInvoiceSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: false),
                    DueDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    NextIssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastIssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringInvoiceSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringInvoiceSchedules_ClientProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "ClientProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ScheduleId",
                table: "Invoices",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status_DueAt",
                table: "Invoices",
                columns: new[] { "Status", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringInvoiceSchedules_IsActive_NextIssueDate",
                table: "RecurringInvoiceSchedules",
                columns: new[] { "IsActive", "NextIssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringInvoiceSchedules_ProjectId",
                table: "RecurringInvoiceSchedules",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_RecurringInvoiceSchedules_ScheduleId",
                table: "Invoices",
                column: "ScheduleId",
                principalTable: "RecurringInvoiceSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_RecurringInvoiceSchedules_ScheduleId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "RecurringInvoiceSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ScheduleId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Status_DueAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LastReminderAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ReminderStage",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "Invoices");
        }
    }
}
