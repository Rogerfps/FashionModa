using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDescuentosYDevolucionesProforma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoMonto",
                table: "Proformas",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoPorcentaje",
                table: "Proformas",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CantidadDevuelta",
                table: "ProformaDetalles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoMonto",
                table: "ProformaDetalles",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoPorcentaje",
                table: "ProformaDetalles",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescuentoMonto",
                table: "Proformas");

            migrationBuilder.DropColumn(
                name: "DescuentoPorcentaje",
                table: "Proformas");

            migrationBuilder.DropColumn(
                name: "CantidadDevuelta",
                table: "ProformaDetalles");

            migrationBuilder.DropColumn(
                name: "DescuentoMonto",
                table: "ProformaDetalles");

            migrationBuilder.DropColumn(
                name: "DescuentoPorcentaje",
                table: "ProformaDetalles");
        }
    }
}
