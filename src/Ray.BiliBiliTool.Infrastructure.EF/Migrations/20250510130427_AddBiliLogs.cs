using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ray.BiliBiliTool.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBiliLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            try
            {
                migrationBuilder.DropTable(name: "bili_logs");
            }
            catch
            {
                // ignored
            }
            migrationBuilder.CreateTable(
                name: "bili_logs",
                columns: table => new
                {
                    id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    timeStamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    level = table.Column<string>(type: "TEXT", nullable: false),
                    exception = table.Column<string>(type: "TEXT", nullable: true),
                    renderedMessage = table.Column<string>(type: "TEXT", nullable: true),
                    properties = table.Column<string>(type: "TEXT", nullable: true),
                    fireInstanceIdComputed = table.Column<string>(
                        type: "TEXT",
                        nullable: true,
                        computedColumnSql: "json_extract(Properties, '$.FireInstanceId')",
                        stored: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bili_logs", x => x.id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Logs_FireInstanceIdComputed",
                table: "bili_logs",
                column: "fireInstanceIdComputed"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bili_logs");
        }
    }
}
