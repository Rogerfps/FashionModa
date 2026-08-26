using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRastreoPedidoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumeroPedidoCliente",
                table: "PedidosProveedorDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PedidoClienteId",
                table: "PedidosProveedorDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidosProveedorDetalle_PedidoClienteId",
                table: "PedidosProveedorDetalle",
                column: "PedidoClienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosProveedorDetalle_PedidosCliente_PedidoClienteId",
                table: "PedidosProveedorDetalle",
                column: "PedidoClienteId",
                principalTable: "PedidosCliente",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidosProveedorDetalle_PedidosCliente_PedidoClienteId",
                table: "PedidosProveedorDetalle");

            migrationBuilder.DropIndex(
                name: "IX_PedidosProveedorDetalle_PedidoClienteId",
                table: "PedidosProveedorDetalle");

            migrationBuilder.DropColumn(
                name: "NumeroPedidoCliente",
                table: "PedidosProveedorDetalle");

            migrationBuilder.DropColumn(
                name: "PedidoClienteId",
                table: "PedidosProveedorDetalle");
        }
    }
}
