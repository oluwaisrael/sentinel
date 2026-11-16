using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentsAndNotificationOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailureCount",
                table: "monitors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveSuccessCount",
                table: "monitors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MonitorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastTransitionAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_incidents_monitors_MonitorId",
                        column: x => x.MonitorId,
                        principalTable: "monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "incident_transitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonitorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransitionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CheckResultId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_transitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_incident_transitions_check_results_CheckResultId",
                        column: x => x.CheckResultId,
                        principalTable: "check_results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_incident_transitions_incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonitorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_outbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_outbox_incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_incident_transitions_CheckResultId",
                table: "incident_transitions",
                column: "CheckResultId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_transitions_IncidentId_TransitionType",
                table: "incident_transitions",
                columns: new[] { "IncidentId", "TransitionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_transitions_MonitorId_OccurredAt",
                table: "incident_transitions",
                columns: new[] { "MonitorId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_incidents_MonitorId",
                table: "incidents",
                column: "MonitorId",
                unique: true,
                filter: "\"Status\" = 'Open'");

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_IncidentId_EventType",
                table: "notification_outbox",
                columns: new[] { "IncidentId", "EventType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_Status_NextAttemptAt_LeaseExpiresAt",
                table: "notification_outbox",
                columns: new[] { "Status", "NextAttemptAt", "LeaseExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incident_transitions");

            migrationBuilder.DropTable(
                name: "notification_outbox");

            migrationBuilder.DropTable(
                name: "incidents");

            migrationBuilder.DropColumn(
                name: "ConsecutiveFailureCount",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "ConsecutiveSuccessCount",
                table: "monitors");
        }
    }
}
