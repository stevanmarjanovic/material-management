using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaterialManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class FiberComposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Free-text composition becomes a JSON list of fibers. Only the values in use so far are known; others are cleared.
            migrationBuilder.Sql("""
                UPDATE Materials SET Composition = CASE lower(trim(Composition))
                    WHEN '100% pamuk' THEN '[{"Fiber":"Cotton","Percent":100}]'
                    WHEN '100% poliester' THEN '[{"Fiber":"Polyester","Percent":100}]'
                    ELSE NULL END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Materials SET Composition = NULL");
        }
    }
}
