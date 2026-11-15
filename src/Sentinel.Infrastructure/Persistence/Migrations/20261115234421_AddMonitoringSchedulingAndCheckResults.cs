using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoringSchedulingAndCheckResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeaseExpiresAt",
                table: "monitors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaseId",
                table: "monitors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextCheckAt",
                table: "monitors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "check_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MonitorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    LatencyMs = table.Column<int>(type: "integer", nullable: true),
                    IsHealthy = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_check_results_monitors_MonitorId",
                        column: x => x.MonitorId,
                        principalTable: "monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_monitors_IsEnabled_NextCheckAt_LeaseExpiresAt",
                table: "monitors",
                columns: new[] { "IsEnabled", "NextCheckAt", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_check_results_MonitorId_CheckedAt",
                table: "check_results",
                columns: new[] { "MonitorId", "CheckedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "check_results");

            migrationBuilder.DropIndex(
                name: "IX_monitors_IsEnabled_NextCheckAt_LeaseExpiresAt",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAt",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "LeaseId",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "NextCheckAt",
                table: "monitors");
        }
    }
}
