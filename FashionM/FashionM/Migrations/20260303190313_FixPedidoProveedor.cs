using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class FixPedidoProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidoClienteDetalles_Proveedores_ProveedorCedula",
                table: "PedidoClienteDetalles");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoClienteDetalles_Proveedores_ProveedorCedula",
                table: "PedidoClienteDetalles",
                column: "ProveedorCedula",
                principalTable: "Proveedores",
                principalColumn: "Cedula",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidoClienteDetalles_Proveedores_ProveedorCedula",
                table: "PedidoClienteDetalles");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoClienteDetalles_Proveedores_ProveedorCedula",
                table: "PedidoClienteDetalles",
                column: "ProveedorCedula",
                principalTable: "Proveedores",
                principalColumn: "Cedula");
        }
    }
}
