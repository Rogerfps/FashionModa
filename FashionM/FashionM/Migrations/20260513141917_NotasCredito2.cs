using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class NotasCredito2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotaCredito_Clientes_ClienteCedula",
                table: "NotaCredito");

            migrationBuilder.DropForeignKey(
                name: "FK_NotaCredito_Empresas_EmpresaId",
                table: "NotaCredito");

            migrationBuilder.DropForeignKey(
                name: "FK_NotaCredito_Ventas_VentaId",
                table: "NotaCredito");

            migrationBuilder.DropForeignKey(
                name: "FK_NotaCreditoDetalle_NotaCredito_NotaCreditoId",
                table: "NotaCreditoDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_NotaCreditoDetalle_VentaDetalles_VentaDetalleId",
                table: "NotaCreditoDetalle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotaCreditoDetalle",
                table: "NotaCreditoDetalle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotaCredito",
                table: "NotaCredito");

            migrationBuilder.RenameTable(
                name: "NotaCreditoDetalle",
                newName: "NotaCreditoDetalles");

            migrationBuilder.RenameTable(
                name: "NotaCredito",
                newName: "NotasCredito");

            migrationBuilder.RenameIndex(
                name: "IX_NotaCreditoDetalle_VentaDetalleId",
                table: "NotaCreditoDetalles",
                newName: "IX_NotaCreditoDetalles_VentaDetalleId");

            migrationBuilder.RenameIndex(
                name: "IX_NotaCreditoDetalle_NotaCreditoId",
                table: "NotaCreditoDetalles",
                newName: "IX_NotaCreditoDetalles_NotaCreditoId");

            migrationBuilder.RenameIndex(
                name: "IX_NotaCredito_VentaId",
                table: "NotasCredito",
                newName: "IX_NotasCredito_VentaId");

            migrationBuilder.RenameIndex(
                name: "IX_NotaCredito_EmpresaId",
                table: "NotasCredito",
                newName: "IX_NotasCredito_EmpresaId");

            migrationBuilder.RenameIndex(
                name: "IX_NotaCredito_ClienteCedula",
                table: "NotasCredito",
                newName: "IX_NotasCredito_ClienteCedula");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotaCreditoDetalles",
                table: "NotaCreditoDetalles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotasCredito",
                table: "NotasCredito",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotaCreditoDetalles_NotasCredito_NotaCreditoId",
                table: "NotaCreditoDetalles",
                column: "NotaCreditoId",
                principalTable: "NotasCredito",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotaCreditoDetalles_VentaDetalles_VentaDetalleId",
                table: "NotaCreditoDetalles",
                column: "VentaDetalleId",
                principalTable: "VentaDetalles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotasCredito_Clientes_ClienteCedula",
                table: "NotasCredito",
                column: "ClienteCedula",
                principalTable: "Clientes",
                principalColumn: "Cedula",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotasCredito_Empresas_EmpresaId",
                table: "NotasCredito",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotasCredito_Ventas_VentaId",
                table: "NotasCredito",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotaCreditoDetalles_NotasCredito_NotaCreditoId",
                table: "NotaCreditoDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_NotaCreditoDetalles_VentaDetalles_VentaDetalleId",
                table: "NotaCreditoDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_NotasCredito_Clientes_ClienteCedula",
                table: "NotasCredito");

            migrationBuilder.DropForeignKey(
                name: "FK_NotasCredito_Empresas_EmpresaId",
                table: "NotasCredito");

            migrationBuilder.DropForeignKey(
                name: "FK_NotasCredito_Ventas_VentaId",
                table: "NotasCredito");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotasCredito",
                table: "NotasCredito");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotaCreditoDetalles",
                table: "NotaCreditoDetalles");

            migrationBuilder.RenameTable(
                name: "NotasCredito",
                newName: "NotaCredito");

            migrationBuilder.RenameTable(
                name: "NotaCreditoDetalles",
                newName: "NotaCreditoDetalle");

            migrationBuilder.RenameIndex(
                name: "IX_NotasCredito_VentaId",
                table: "NotaCredito",
                newName: "IX_NotaCredito_VentaId");

            migrationBuilder.RenameIndex(
                name: "IX_NotasCredito_EmpresaId",
                table: "NotaCredito",
                newName: "IX_NotaCredito_EmpresaId");

            migrationBuilder.RenameIndex(
                name: "IX_NotasCredito_ClienteCedula",
                table: "NotaCredito",
                newName: "IX_NotaCredito_ClienteCedula");

            migrationBuilder.RenameIndex(
                name: "IX_NotaCreditoDetalles_VentaDetalleId",
                table: "NotaCreditoDetalle",
                newName: "IX_NotaCreditoDetalle_VentaDetalleId");

            migrationBuilder.RenameIndex(
                name: "IX_NotaCreditoDetalles_NotaCreditoId",
                table: "NotaCreditoDetalle",
                newName: "IX_NotaCreditoDetalle_NotaCreditoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotaCredito",
                table: "NotaCredito",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotaCreditoDetalle",
                table: "NotaCreditoDetalle",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotaCredito_Clientes_ClienteCedula",
                table: "NotaCredito",
                column: "ClienteCedula",
                principalTable: "Clientes",
                principalColumn: "Cedula",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotaCredito_Empresas_EmpresaId",
                table: "NotaCredito",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotaCredito_Ventas_VentaId",
                table: "NotaCredito",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotaCreditoDetalle_NotaCredito_NotaCreditoId",
                table: "NotaCreditoDetalle",
                column: "NotaCreditoId",
                principalTable: "NotaCredito",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotaCreditoDetalle_VentaDetalles_VentaDetalleId",
                table: "NotaCreditoDetalle",
                column: "VentaDetalleId",
                principalTable: "VentaDetalles",
                principalColumn: "Id");
        }
    }
}
