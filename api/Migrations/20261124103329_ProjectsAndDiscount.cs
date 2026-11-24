using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaterialManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProjectsAndDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Materials",
                newName: "PricePerMeter");

            migrationBuilder.AddColumn<double>(
                name: "DiscountPercent",
                table: "Materials",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LengthCm",
                table: "Materials",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            // Bought length = length of each material's first piece.
            migrationBuilder.Sql("UPDATE Materials SET LengthCm = (SELECT json_extract(Shape, '$[0].H') FROM Pieces WHERE MaterialId = Materials.Id ORDER BY Id LIMIT 1)");

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Cuts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Photo = table.Column<byte[]>(type: "BLOB", nullable: true),
                    PhotoContentType = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cuts_ProjectId",
                table: "Cuts",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cuts_Projects_ProjectId",
                table: "Cuts",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cuts_Projects_ProjectId",
                table: "Cuts");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Cuts_ProjectId",
                table: "Cuts");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "LengthCm",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Cuts");

            migrationBuilder.RenameColumn(
                name: "PricePerMeter",
                table: "Materials",
                newName: "Price");
        }
    }
}
