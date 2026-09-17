using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ray.BiliBiliTool.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 注意：EF 曾在本迁移里自动生成
            //   migrationBuilder.RenameColumn(name: "id", table: "bili_user", newName: "Id")
            // 这是误报：仓库里的模型快照残留了 User.Id 的 HasColumnName("id")，但实体上没有该映射，
            // 而且物理库的 bili_user 自 AddUser 迁移起就是 "Id"（大写），从来没有过小写 id 列。
            // 保留这条语句会让迁移在 "no such column: id" 处抛错，导致应用启动失败，因此删除。
            // 快照已随之修正为 "Id"，与模型和物理库一致。
            migrationBuilder.CreateTable(
                name: "bili_task_records",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    TaskKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TaskItemKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    RecordDate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Trigger = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bili_task_records", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_bili_task_records_UserId_TaskKey_RecordDate",
                table: "bili_task_records",
                columns: new[] { "UserId", "TaskKey", "RecordDate" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bili_task_records");
        }
    }
}
