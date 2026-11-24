using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaterialManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class Stores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StorePurchaseId",
                table: "Purchases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Materials",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Website = table.Column<string>(type: "TEXT", nullable: true),
                    WorkingHours = table.Column<string>(type: "TEXT", nullable: true),
                    PurchaseUrlTemplate = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.Id);
                });

            // Store names typed on materials become stores; materials link to them by name.
            migrationBuilder.Sql("INSERT INTO Stores (Name) SELECT DISTINCT trim(Store) FROM Materials WHERE trim(coalesce(Store, '')) <> ''");

            migrationBuilder.Sql("UPDATE Materials SET StoreId = (SELECT Id FROM Stores WHERE Stores.Name = trim(Materials.Store))");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_StoreId",
                table: "Materials",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Stores_StoreId",
                table: "Materials",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropColumn(
                name: "Store",
                table: "Materials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Stores_StoreId",
                table: "Materials");

            migrationBuilder.DropTable(
                name: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_Materials_StoreId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "StorePurchaseId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Materials");

            migrationBuilder.AddColumn<string>(
                name: "Store",
                table: "Materials",
                type: "TEXT",
                nullable: true);
        }
    }
}
