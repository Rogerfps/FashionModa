using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class NotasCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotaCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VentaId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: false),
                    TipoDocumento = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    ClienteCedula = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    TotalDevuelto = table.Column<decimal>(type: "numeric", nullable: false),
                    Semana = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    Año = table.Column<int>(type: "integer", nullable: false),
                    AgenteVenta = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaCredito", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaCredito_Clientes_ClienteCedula",
                        column: x => x.ClienteCedula,
                        principalTable: "Clientes",
                        principalColumn: "Cedula",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotaCredito_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotaCredito_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotaCreditoDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotaCreditoId = table.Column<int>(type: "integer", nullable: false),
                    InventarioCodigo = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Talla = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    VentaDetalleId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaCreditoDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaCreditoDetalle_NotaCredito_NotaCreditoId",
                        column: x => x.NotaCreditoId,
                        principalTable: "NotaCredito",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotaCreditoDetalle_VentaDetalles_VentaDetalleId",
                        column: x => x.VentaDetalleId,
                        principalTable: "VentaDetalles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotaCredito_ClienteCedula",
                table: "NotaCredito",
                column: "ClienteCedula");

            migrationBuilder.CreateIndex(
                name: "IX_NotaCredito_EmpresaId",
                table: "NotaCredito",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaCredito_VentaId",
                table: "NotaCredito",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaCreditoDetalle_NotaCreditoId",
                table: "NotaCreditoDetalle",
                column: "NotaCreditoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaCreditoDetalle_VentaDetalleId",
                table: "NotaCreditoDetalle",
                column: "VentaDetalleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotaCreditoDetalle");

            migrationBuilder.DropTable(
                name: "NotaCredito");
        }
    }
}
