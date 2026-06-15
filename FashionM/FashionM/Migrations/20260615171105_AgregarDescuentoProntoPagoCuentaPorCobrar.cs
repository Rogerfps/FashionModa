using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDescuentoProntoPagoCuentaPorCobrar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoAplicado",
                table: "CuentasPorCobrar",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "DescuentoOtorgado",
                table: "CuentasPorCobrar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaDescuento",
                table: "CuentasPorCobrar",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescuentoAplicado",
                table: "CuentasPorCobrar");

            migrationBuilder.DropColumn(
                name: "DescuentoOtorgado",
                table: "CuentasPorCobrar");

            migrationBuilder.DropColumn(
                name: "FechaDescuento",
                table: "CuentasPorCobrar");
        }
    }
}
