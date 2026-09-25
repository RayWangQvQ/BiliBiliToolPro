using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ray.BiliBiliTool.Web.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAfterQuartz322 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EXECUTION_GROUP",
                table: "QRTZ_TRIGGERS",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PREFERRED_NODE",
                table: "QRTZ_TRIGGERS",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "PREFERRED_NODE_AUTO",
                table: "QRTZ_TRIGGERS",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<int>(
                name: "RETRY_ATTEMPT",
                table: "QRTZ_TRIGGERS",
                type: "integer",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "RETRY_POLICY",
                table: "QRTZ_TRIGGERS",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "EXECUTION_GROUP",
                table: "QRTZ_FIRED_TRIGGERS",
                type: "text",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "QRTZ_PAUSED_JOB_GRPS",
                columns: table => new
                {
                    SCHED_NAME = table.Column<string>(type: "text", nullable: false),
                    JOB_GROUP = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_QRTZ_PAUSED_JOB_GRPS",
                        x => new { x.SCHED_NAME, x.JOB_GROUP }
                    );
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "QRTZ_PAUSED_JOB_GRPS");

            migrationBuilder.DropColumn(name: "EXECUTION_GROUP", table: "QRTZ_TRIGGERS");

            migrationBuilder.DropColumn(name: "PREFERRED_NODE", table: "QRTZ_TRIGGERS");

            migrationBuilder.DropColumn(name: "PREFERRED_NODE_AUTO", table: "QRTZ_TRIGGERS");

            migrationBuilder.DropColumn(name: "RETRY_ATTEMPT", table: "QRTZ_TRIGGERS");

            migrationBuilder.DropColumn(name: "RETRY_POLICY", table: "QRTZ_TRIGGERS");

            migrationBuilder.DropColumn(name: "EXECUTION_GROUP", table: "QRTZ_FIRED_TRIGGERS");
        }
    }
}
