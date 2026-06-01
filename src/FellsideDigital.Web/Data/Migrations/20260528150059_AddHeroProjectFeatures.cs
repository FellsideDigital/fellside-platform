using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FellsideDigital.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHeroProjectFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HeroDisplayOrder",
                table: "ClientProjects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HeroShowcaseUrl",
                table: "ClientProjects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroTagline",
                table: "ClientProjects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHeroProject",
                table: "ClientProjects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ScreenshotPath",
                table: "ClientProjects",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectIntegrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectIntegrations_ClientProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "ClientProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Style = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMetrics_ClientProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "ClientProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPipelineSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepType = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPipelineSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPipelineSteps_ClientProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "ClientProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectIntegrations_ProjectId",
                table: "ProjectIntegrations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMetrics_ProjectId",
                table: "ProjectMetrics",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPipelineSteps_ProjectId",
                table: "ProjectPipelineSteps",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectIntegrations");

            migrationBuilder.DropTable(
                name: "ProjectMetrics");

            migrationBuilder.DropTable(
                name: "ProjectPipelineSteps");

            migrationBuilder.DropColumn(
                name: "HeroDisplayOrder",
                table: "ClientProjects");

            migrationBuilder.DropColumn(
                name: "HeroShowcaseUrl",
                table: "ClientProjects");

            migrationBuilder.DropColumn(
                name: "HeroTagline",
                table: "ClientProjects");

            migrationBuilder.DropColumn(
                name: "IsHeroProject",
                table: "ClientProjects");

            migrationBuilder.DropColumn(
                name: "ScreenshotPath",
                table: "ClientProjects");
        }
    }
}
