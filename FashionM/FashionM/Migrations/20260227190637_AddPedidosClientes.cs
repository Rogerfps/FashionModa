using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AddPedidosClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PedidosCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteCedula = table.Column<int>(type: "integer", nullable: false),
                    FechaPedido = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    Vendedor = table.Column<string>(type: "text", nullable: false),
                    FirmaCredito = table.Column<bool>(type: "boolean", nullable: false),
                    FirmaBodega = table.Column<bool>(type: "boolean", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosCliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosCliente_Clientes_ClienteCedula",
                        column: x => x.ClienteCedula,
                        principalTable: "Clientes",
                        principalColumn: "Cedula",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoClienteDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoClienteId = table.Column<int>(type: "integer", nullable: false),
                    CodigoProducto = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Talla = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoClienteDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoClienteDetalles_PedidosCliente_PedidoClienteId",
                        column: x => x.PedidoClienteId,
                        principalTable: "PedidosCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoClienteDetalles_PedidoClienteId",
                table: "PedidoClienteDetalles",
                column: "PedidoClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCliente_ClienteCedula",
                table: "PedidosCliente",
                column: "ClienteCedula");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidoClienteDetalles");

            migrationBuilder.DropTable(
                name: "PedidosCliente");
        }
    }
}
