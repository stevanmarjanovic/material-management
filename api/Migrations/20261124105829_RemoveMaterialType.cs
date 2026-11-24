using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaterialManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMaterialType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Materials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Materials",
                type: "TEXT",
                nullable: true);
        }
    }
}
