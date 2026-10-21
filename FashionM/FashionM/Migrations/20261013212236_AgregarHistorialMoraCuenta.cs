using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AgregarHistorialMoraCuenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiasMoraFinal",
                table: "CuentasPorCobrar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCancelacion",
                table: "CuentasPorCobrar",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiasMoraFinal",
                table: "CuentasPorCobrar");

            migrationBuilder.DropColumn(
                name: "FechaCancelacion",
                table: "CuentasPorCobrar");
        }
    }
}
