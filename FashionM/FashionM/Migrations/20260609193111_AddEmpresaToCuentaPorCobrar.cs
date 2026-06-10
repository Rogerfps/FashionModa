using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresaToCuentaPorCobrar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "CuentasPorCobrar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPorCobrar_EmpresaId",
                table: "CuentasPorCobrar",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_CuentasPorCobrar_Empresas_EmpresaId",
                table: "CuentasPorCobrar",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuentasPorCobrar_Empresas_EmpresaId",
                table: "CuentasPorCobrar");

            migrationBuilder.DropIndex(
                name: "IX_CuentasPorCobrar_EmpresaId",
                table: "CuentasPorCobrar");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "CuentasPorCobrar");
        }
    }
}
