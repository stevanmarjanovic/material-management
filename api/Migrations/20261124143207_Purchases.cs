using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaterialManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class Purchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    LengthCm = table.Column<double>(type: "REAL", nullable: false),
                    PricePerMeter = table.Column<double>(type: "REAL", nullable: true),
                    DiscountPercent = table.Column<double>(type: "REAL", nullable: true),
                    PurchaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Purchases_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Each existing material was one purchase: move its length, price, discount and date there.
            migrationBuilder.Sql("INSERT INTO Purchases (MaterialId, LengthCm, PricePerMeter, DiscountPercent, PurchaseDate) SELECT Id, LengthCm, PricePerMeter, DiscountPercent, PurchaseDate FROM Materials ORDER BY Id");

            migrationBuilder.AddColumn<int>(
                name: "PurchaseId",
                table: "Pieces",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseId",
                table: "Cuts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // All pieces and cuts of a material come from its (single) purchase.
            migrationBuilder.Sql("UPDATE Pieces SET PurchaseId = (SELECT Id FROM Purchases WHERE Purchases.MaterialId = Pieces.MaterialId)");

            migrationBuilder.Sql("UPDATE Cuts SET PurchaseId = (SELECT Id FROM Purchases WHERE Purchases.MaterialId = Cuts.MaterialId)");

            migrationBuilder.CreateIndex(
                name: "IX_Pieces_PurchaseId",
                table: "Pieces",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuts_PurchaseId",
                table: "Cuts",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_MaterialId",
                table: "Purchases",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cuts_Purchases_PurchaseId",
                table: "Cuts",
                column: "PurchaseId",
                principalTable: "Purchases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pieces_Purchases_PurchaseId",
                table: "Pieces",
                column: "PurchaseId",
                principalTable: "Purchases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "LengthCm",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "PricePerMeter",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "Materials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cuts_Purchases_PurchaseId",
                table: "Cuts");

            migrationBuilder.DropForeignKey(
                name: "FK_Pieces_Purchases_PurchaseId",
                table: "Pieces");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Pieces_PurchaseId",
                table: "Pieces");

            migrationBuilder.DropIndex(
                name: "IX_Cuts_PurchaseId",
                table: "Cuts");

            migrationBuilder.DropColumn(
                name: "PurchaseId",
                table: "Pieces");

            migrationBuilder.DropColumn(
                name: "PurchaseId",
                table: "Cuts");

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

            migrationBuilder.AddColumn<double>(
                name: "PricePerMeter",
                table: "Materials",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PurchaseDate",
                table: "Materials",
                type: "TEXT",
                nullable: true);
        }
    }
}
