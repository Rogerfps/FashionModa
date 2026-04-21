using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class PedidoProveedorCatalogoFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidoClienteDetalles_Proveedores_ProveedorCedula",
                table: "PedidoClienteDetalles");

            migrationBuilder.RenameColumn(
                name: "ProveedorCedula",
                table: "PedidoClienteDetalles",
                newName: "ProveedorCatalogoId");

            migrationBuilder.RenameIndex(
                name: "IX_PedidoClienteDetalles_ProveedorCedula",
                table: "PedidoClienteDetalles",
                newName: "IX_PedidoClienteDetalles_ProveedorCatalogoId");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioVenta",
                table: "ZapatosProveedor",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioCosto",
                table: "ZapatosProveedor",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioColombia",
                table: "ZapatosProveedor",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoClienteDetalles_ProveedoresCatalogo_ProveedorCatalogo~",
                table: "PedidoClienteDetalles",
                column: "ProveedorCatalogoId",
                principalTable: "ProveedoresCatalogo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidoClienteDetalles_ProveedoresCatalogo_ProveedorCatalogo~",
                table: "PedidoClienteDetalles");

            migrationBuilder.RenameColumn(
                name: "ProveedorCatalogoId",
                table: "PedidoClienteDetalles",
                newName: "ProveedorCedula");

            migrationBuilder.RenameIndex(
                name: "IX_PedidoClienteDetalles_ProveedorCatalogoId",
                table: "PedidoClienteDetalles",
                newName: "IX_PedidoClienteDetalles_ProveedorCedula");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioVenta",
                table: "ZapatosProveedor",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioCosto",
                table: "ZapatosProveedor",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioColombia",
                table: "ZapatosProveedor",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoClienteDetalles_Proveedores_ProveedorCedula",
                table: "PedidoClienteDetalles",
                column: "ProveedorCedula",
                principalTable: "Proveedores",
                principalColumn: "Cedula",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
