using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotasCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PrecioUnitario",
                table: "NotaCreditoDetalles",
                newName: "PrecioOriginal");

            migrationBuilder.RenameColumn(
                name: "Cantidad",
                table: "NotaCreditoDetalles",
                newName: "CantidadOriginal");

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoGlobal",
                table: "NotasCredito",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "NotasCredito",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CantidadDevuelta",
                table: "NotaCreditoDetalles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoLinea",
                table: "NotaCreditoDetalles",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "NotaCreditoDetalles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "NotaCreditoDetalles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioCorregido",
                table: "NotaCreditoDetalles",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescuentoGlobal",
                table: "NotasCredito");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "NotasCredito");

            migrationBuilder.DropColumn(
                name: "CantidadDevuelta",
                table: "NotaCreditoDetalles");

            migrationBuilder.DropColumn(
                name: "DescuentoLinea",
                table: "NotaCreditoDetalles");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "NotaCreditoDetalles");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "NotaCreditoDetalles");

            migrationBuilder.DropColumn(
                name: "PrecioCorregido",
                table: "NotaCreditoDetalles");

            migrationBuilder.RenameColumn(
                name: "PrecioOriginal",
                table: "NotaCreditoDetalles",
                newName: "PrecioUnitario");

            migrationBuilder.RenameColumn(
                name: "CantidadOriginal",
                table: "NotaCreditoDetalles",
                newName: "Cantidad");
        }
    }
}
