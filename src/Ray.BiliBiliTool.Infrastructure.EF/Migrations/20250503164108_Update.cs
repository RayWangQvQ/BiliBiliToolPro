using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ray.BiliBiliTool.Web.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bili_execution_logs",
                columns: table => new
                {
                    LogId = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RunInstanceId = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: true
                    ),
                    LogType = table.Column<string>(type: "varchar(20)", nullable: false),
                    JobName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    JobGroup = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    TriggerName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: true
                    ),
                    TriggerGroup = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: true
                    ),
                    ScheduleFireTimeUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    FireTimeUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    JobRunTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Result = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    ErrorMessage = table.Column<string>(
                        type: "TEXT",
                        maxLength: 8000,
                        nullable: true
                    ),
                    IsVetoed = table.Column<bool>(type: "INTEGER", nullable: true),
                    IsException = table.Column<bool>(type: "INTEGER", nullable: true),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: true),
                    ReturnCode = table.Column<string>(type: "TEXT", maxLength: 28, nullable: true),
                    DateAddedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bili_execution_logs", x => x.LogId);
                }
            );

            migrationBuilder.CreateTable(
                name: "bili_execution_log_details",
                columns: table => new
                {
                    LogId = table.Column<long>(type: "INTEGER", nullable: false),
                    ExecutionDetails = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorStackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorCode = table.Column<int>(type: "INTEGER", nullable: true),
                    ErrorHelpLink = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1000,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bili_execution_log_details", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_bili_execution_log_details_bili_execution_logs_LogId",
                        column: x => x.LogId,
                        principalTable: "bili_execution_logs",
                        principalColumn: "LogId",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_bili_execution_logs_DateAddedUtc_LogType",
                table: "bili_execution_logs",
                columns: new[] { "DateAddedUtc", "LogType" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_bili_execution_logs_RunInstanceId",
                table: "bili_execution_logs",
                column: "RunInstanceId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_bili_execution_logs_TriggerName_TriggerGroup_JobName_JobGroup_DateAddedUtc",
                table: "bili_execution_logs",
                columns: new[]
                {
                    "TriggerName",
                    "TriggerGroup",
                    "JobName",
                    "JobGroup",
                    "DateAddedUtc",
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bili_execution_log_details");

            migrationBuilder.DropTable(name: "bili_execution_logs");
        }
    }
}
