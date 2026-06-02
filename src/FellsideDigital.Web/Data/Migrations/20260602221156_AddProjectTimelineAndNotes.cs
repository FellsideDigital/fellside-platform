using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FellsideDigital.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTimelineAndNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "text", nullable: true),
                    AuthorName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectNotes_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectNotes_ClientProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "ClientProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: true),
                    ActorName = table.Column<string>(type: "text", nullable: true),
                    NoteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Data = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTimelineEvents_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectTimelineEvents_ClientProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "ClientProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTimelineEvents_ProjectNotes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "ProjectNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotes_AuthorId",
                table: "ProjectNotes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotes_ProjectId",
                table: "ProjectNotes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimelineEvents_ActorId",
                table: "ProjectTimelineEvents",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimelineEvents_NoteId",
                table: "ProjectTimelineEvents",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTimelineEvents_ProjectId_OccurredAt",
                table: "ProjectTimelineEvents",
                columns: new[] { "ProjectId", "OccurredAt" });

            // ── Backfill: migrate existing project activity into the new model ──
            // Enum int values — TimelineEventType: ProjectCreated=0, StatusChanged=1,
            // MilestoneCompleted=3, ProjectCompleted=4, NoteAdded=6, InvoiceCreated=7,
            // InvoicePaid=9. TimelineVisibility: ClientVisible=1. ProjectStatus.Completed=4,
            // PhaseStatus.Completed=4, InvoiceStatus.Paid=2.
            // Author/actor display name, snapshotted from AspNetUsers.
            const string DisplayName =
                "COALESCE(NULLIF(TRIM(CONCAT(u.\"FirstName\", ' ', u.\"LastName\")), ''), u.\"CompanyName\", u.\"Email\")";

            // 1. Old client-facing messages → client-visible notes (reuse the status-update Id
            //    as the note Id so the matching NoteAdded event can correlate to it).
            migrationBuilder.Sql($@"
                INSERT INTO ""ProjectNotes"" (""Id"", ""Body"", ""Visibility"", ""CreatedAt"", ""UpdatedAt"", ""ProjectId"", ""AuthorId"", ""AuthorName"")
                SELECT su.""Id"", su.""Message"", 1, su.""CreatedAt"", su.""CreatedAt"", su.""ProjectId"", su.""CreatedByAdminId"", COALESCE({DisplayName}, '')
                FROM ""ProjectStatusUpdates"" su
                JOIN ""AspNetUsers"" u ON u.""Id"" = su.""CreatedByAdminId""
                WHERE TRIM(su.""Message"") <> '';");

            // 2. NoteAdded events for those migrated notes.
            migrationBuilder.Sql($@"
                INSERT INTO ""ProjectTimelineEvents"" (""Id"", ""Type"", ""Summary"", ""Visibility"", ""OccurredAt"", ""ProjectId"", ""ActorId"", ""ActorName"", ""NoteId"", ""Data"")
                SELECT gen_random_uuid(), 6, 'Note added', 1, su.""CreatedAt"", su.""ProjectId"", su.""CreatedByAdminId"", {DisplayName}, su.""Id"", NULL
                FROM ""ProjectStatusUpdates"" su
                JOIN ""AspNetUsers"" u ON u.""Id"" = su.""CreatedByAdminId""
                WHERE TRIM(su.""Message"") <> '';");

            // 3. Status-change events from old updates that carried a NewStatus.
            migrationBuilder.Sql($@"
                INSERT INTO ""ProjectTimelineEvents"" (""Id"", ""Type"", ""Summary"", ""Visibility"", ""OccurredAt"", ""ProjectId"", ""ActorId"", ""ActorName"", ""NoteId"", ""Data"")
                SELECT gen_random_uuid(),
                       CASE WHEN su.""NewStatus"" = 4 THEN 4 ELSE 1 END,
                       CASE WHEN su.""NewStatus"" = 4 THEN 'Project completed'
                            ELSE 'Status changed to ' || (CASE su.""NewStatus""
                                WHEN 0 THEN 'Pending' WHEN 1 THEN 'In Progress'
                                WHEN 2 THEN 'Blocked' WHEN 3 THEN 'On Hold' ELSE '' END)
                       END,
                       1, su.""CreatedAt"", su.""ProjectId"", su.""CreatedByAdminId"", {DisplayName}, NULL, NULL
                FROM ""ProjectStatusUpdates"" su
                JOIN ""AspNetUsers"" u ON u.""Id"" = su.""CreatedByAdminId""
                WHERE su.""NewStatus"" IS NOT NULL;");

            // 4. ProjectCreated event for every existing project.
            migrationBuilder.Sql($@"
                INSERT INTO ""ProjectTimelineEvents"" (""Id"", ""Type"", ""Summary"", ""Visibility"", ""OccurredAt"", ""ProjectId"", ""ActorId"", ""ActorName"", ""NoteId"", ""Data"")
                SELECT gen_random_uuid(), 0, 'Project created', 1, p.""CreatedAt"", p.""Id"", p.""CreatedByAdminId"", {DisplayName}, NULL, NULL
                FROM ""ClientProjects"" p
                JOIN ""AspNetUsers"" u ON u.""Id"" = p.""CreatedByAdminId"";");

            // 5. Invoice events: created (always) and paid (where settled).
            migrationBuilder.Sql(@"
                INSERT INTO ""ProjectTimelineEvents"" (""Id"", ""Type"", ""Summary"", ""Visibility"", ""OccurredAt"", ""ProjectId"", ""ActorId"", ""ActorName"", ""NoteId"", ""Data"")
                SELECT gen_random_uuid(), 7, 'Invoice issued: ' || i.""Title"", 1, i.""IssuedAt"", i.""ProjectId"", NULL, NULL, NULL, NULL
                FROM ""Invoices"" i;");
            migrationBuilder.Sql(@"
                INSERT INTO ""ProjectTimelineEvents"" (""Id"", ""Type"", ""Summary"", ""Visibility"", ""OccurredAt"", ""ProjectId"", ""ActorId"", ""ActorName"", ""NoteId"", ""Data"")
                SELECT gen_random_uuid(), 9, 'Invoice paid: ' || i.""Title"", 1, i.""PaidAt"", i.""ProjectId"", NULL, NULL, NULL, NULL
                FROM ""Invoices"" i
                WHERE i.""Status"" = 2 AND i.""PaidAt"" IS NOT NULL;");

            // 6. MilestoneCompleted event for every completed plan phase.
            migrationBuilder.Sql(@"
                INSERT INTO ""ProjectTimelineEvents"" (""Id"", ""Type"", ""Summary"", ""Visibility"", ""OccurredAt"", ""ProjectId"", ""ActorId"", ""ActorName"", ""NoteId"", ""Data"")
                SELECT gen_random_uuid(), 3, 'Milestone completed: ' || ph.""Title"", 1, ph.""UpdatedAt"", ph.""ProjectId"", NULL, NULL, NULL, NULL
                FROM ""ProjectPlanPhases"" ph
                WHERE ph.""Status"" = 4;");

            // Old activity is now migrated — drop the legacy table.
            migrationBuilder.DropTable(
                name: "ProjectStatusUpdates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectTimelineEvents");

            migrationBuilder.DropTable(
                name: "ProjectNotes");

            migrationBuilder.CreateTable(
                name: "ProjectStatusUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByAdminId = table.Column<string>(type: "text", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStatusUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectStatusUpdates_AspNetUsers_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectStatusUpdates_ClientProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "ClientProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatusUpdates_CreatedByAdminId",
                table: "ProjectStatusUpdates",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatusUpdates_ProjectId",
                table: "ProjectStatusUpdates",
                column: "ProjectId");
        }
    }
}
