using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FellsideDigital.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisitorEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    Browser = table.Column<string>(type: "text", nullable: false),
                    IpHash = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true),
                    Timezone = table.Column<string>(type: "text", nullable: true),
                    ScreenWidth = table.Column<int>(type: "integer", nullable: true),
                    ScreenHeight = table.Column<int>(type: "integer", nullable: true),
                    ViewportWidth = table.Column<int>(type: "integer", nullable: true),
                    ViewportHeight = table.Column<int>(type: "integer", nullable: true),
                    Referrer = table.Column<string>(type: "text", nullable: true),
                    UtmSource = table.Column<string>(type: "text", nullable: true),
                    UtmMedium = table.Column<string>(type: "text", nullable: true),
                    UtmCampaign = table.Column<string>(type: "text", nullable: true),
                    EngagementSeconds = table.Column<int>(type: "integer", nullable: true),
                    ScrollDepthPercent = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitorEvents_OccurredAt",
                table: "VisitorEvents",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitorEvents");
        }
    }
}
