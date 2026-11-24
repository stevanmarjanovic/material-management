using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaterialManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class RecalculatePreviewColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Colors were averaged over the whole photo; clear them so startup recalculates from the center square.
            migrationBuilder.Sql("UPDATE Materials SET PreviewColor = NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
