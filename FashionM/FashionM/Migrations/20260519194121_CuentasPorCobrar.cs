using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class CuentasPorCobrar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CuentasPorCobrar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VentaId = table.Column<int>(type: "integer", nullable: false),
                    ClienteCedula = table.Column<int>(type: "integer", nullable: false),
                    MontoOriginal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasPorCobrar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuentasPorCobrar_Clientes_ClienteCedula",
                        column: x => x.ClienteCedula,
                        principalTable: "Clientes",
                        principalColumn: "Cedula",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuentasPorCobrar_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CuentasPorCobrarPagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CuentaPorCobrarId = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MetodoPago = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FotoRecibo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasPorCobrarPagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuentasPorCobrarPagos_CuentasPorCobrar_CuentaPorCobrarId",
                        column: x => x.CuentaPorCobrarId,
                        principalTable: "CuentasPorCobrar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPorCobrar_ClienteCedula",
                table: "CuentasPorCobrar",
                column: "ClienteCedula");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPorCobrar_VentaId",
                table: "CuentasPorCobrar",
                column: "VentaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPorCobrarPagos_CuentaPorCobrarId",
                table: "CuentasPorCobrarPagos",
                column: "CuentaPorCobrarId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuentasPorCobrarPagos");

            migrationBuilder.DropTable(
                name: "CuentasPorCobrar");
        }
    }
}
