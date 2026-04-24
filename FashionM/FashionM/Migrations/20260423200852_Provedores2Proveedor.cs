using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class Provedores2Proveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidosProveedor_Proveedores_ProveedorCedula",
                table: "PedidosProveedor");

            migrationBuilder.RenameColumn(
                name: "ProveedorCedula",
                table: "PedidosProveedor",
                newName: "ProveedorCatalogoId");

            migrationBuilder.RenameIndex(
                name: "IX_PedidosProveedor_ProveedorCedula",
                table: "PedidosProveedor",
                newName: "IX_PedidosProveedor_ProveedorCatalogoId");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosProveedor_ProveedoresCatalogo_ProveedorCatalogoId",
                table: "PedidosProveedor",
                column: "ProveedorCatalogoId",
                principalTable: "ProveedoresCatalogo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidosProveedor_ProveedoresCatalogo_ProveedorCatalogoId",
                table: "PedidosProveedor");

            migrationBuilder.RenameColumn(
                name: "ProveedorCatalogoId",
                table: "PedidosProveedor",
                newName: "ProveedorCedula");

            migrationBuilder.RenameIndex(
                name: "IX_PedidosProveedor_ProveedorCatalogoId",
                table: "PedidosProveedor",
                newName: "IX_PedidosProveedor_ProveedorCedula");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosProveedor_Proveedores_ProveedorCedula",
                table: "PedidosProveedor",
                column: "ProveedorCedula",
                principalTable: "Proveedores",
                principalColumn: "Cedula",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
