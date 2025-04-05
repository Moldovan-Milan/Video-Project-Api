using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmegaStreamServices.Migrations
{
    /// <inheritdoc />
    public partial class Short : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_short",
                table: "videos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_short",
                table: "videos");
        }
    }
}
