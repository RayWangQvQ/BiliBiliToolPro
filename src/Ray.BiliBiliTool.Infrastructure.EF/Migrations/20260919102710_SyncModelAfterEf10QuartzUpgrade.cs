using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ray.BiliBiliTool.Web.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAfterEf10QuartzUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "id", table: "bili_user", newName: "Id");

            migrationBuilder.AddColumn<long>(
                name: "MISFIRE_ORIG_FIRE_TIME",
                table: "QRTZ_TRIGGERS",
                type: "bigint",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MISFIRE_ORIG_FIRE_TIME", table: "QRTZ_TRIGGERS");

            migrationBuilder.RenameColumn(name: "Id", table: "bili_user", newName: "id");
        }
    }
}
