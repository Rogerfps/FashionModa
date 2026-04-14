using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposZapato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Zapatos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Cantidad",
                table: "Zapatos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Detalle",
                table: "Zapatos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Numero",
                table: "Zapatos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioCosto",
                table: "Zapatos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioVenta",
                table: "Zapatos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Zapatos");

            migrationBuilder.DropColumn(
                name: "Cantidad",
                table: "Zapatos");

            migrationBuilder.DropColumn(
                name: "Detalle",
                table: "Zapatos");

            migrationBuilder.DropColumn(
                name: "Numero",
                table: "Zapatos");

            migrationBuilder.DropColumn(
                name: "PrecioCosto",
                table: "Zapatos");

            migrationBuilder.DropColumn(
                name: "PrecioVenta",
                table: "Zapatos");
        }
    }
}
