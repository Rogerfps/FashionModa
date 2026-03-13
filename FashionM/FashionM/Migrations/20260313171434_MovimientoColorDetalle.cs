using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class MovimientoColorDetalle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "MovimientosDetalle",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Detalle",
                table: "MovimientosDetalle",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "MovimientosDetalle");

            migrationBuilder.DropColumn(
                name: "Detalle",
                table: "MovimientosDetalle");
        }
    }
}
