using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class FixHistorialInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistorialInventarios_Inventarios_CodigoInventario",
                table: "HistorialInventarios");

            migrationBuilder.DropIndex(
                name: "IX_HistorialInventarios_CodigoInventario",
                table: "HistorialInventarios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_HistorialInventarios_CodigoInventario",
                table: "HistorialInventarios",
                column: "CodigoInventario");

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialInventarios_Inventarios_CodigoInventario",
                table: "HistorialInventarios",
                column: "CodigoInventario",
                principalTable: "Inventarios",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
