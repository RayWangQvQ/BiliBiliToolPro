using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ray.BiliTool.Repository.Migrations
{
    /// <inheritdoc />
    public partial class addenable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Enable",
                table: "DbConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Enable",
                table: "DbConfigs");
        }
    }
}
