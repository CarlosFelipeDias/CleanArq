using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArq.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Name", "Description", "Price", "Stock", "ImageUrl", "CategoryId" },
                values: new object[,]
                {
                    { 1, "Spiral notebook", "100-sheet spiral notebook", 7.45m, 50, "notebook1.jpg", 2 },
                    { 2, "School pencil case", "Gray school pencil case", 5.65m, 70, "pencil-case1.jpg", 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValues: new object[] { 1, 2 });
        }
    }
}
