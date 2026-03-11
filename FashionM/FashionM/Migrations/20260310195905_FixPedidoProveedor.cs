using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class FixPedidoProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PedidosMain",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Semana = table.Column<int>(type: "integer", nullable: false),
                    FechaGenerado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosMain", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PedidosProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoMainId = table.Column<int>(type: "integer", nullable: false),
                    Empresa = table.Column<string>(type: "text", nullable: false),
                    ProveedorCedula = table.Column<int>(type: "integer", nullable: false),
                    FechaPedido = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosProveedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosProveedor_PedidosMain_PedidoMainId",
                        column: x => x.PedidoMainId,
                        principalTable: "PedidosMain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidosProveedor_Proveedores_ProveedorCedula",
                        column: x => x.ProveedorCedula,
                        principalTable: "Proveedores",
                        principalColumn: "Cedula",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidosProveedorDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoProveedorId = table.Column<int>(type: "integer", nullable: false),
                    CodigoProducto = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Talla = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosProveedorDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosProveedorDetalle_PedidosProveedor_PedidoProveedorId",
                        column: x => x.PedidoProveedorId,
                        principalTable: "PedidosProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosProveedor_PedidoMainId",
                table: "PedidosProveedor",
                column: "PedidoMainId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosProveedor_ProveedorCedula",
                table: "PedidosProveedor",
                column: "ProveedorCedula");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosProveedorDetalle_PedidoProveedorId",
                table: "PedidosProveedorDetalle",
                column: "PedidoProveedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidosProveedorDetalle");

            migrationBuilder.DropTable(
                name: "PedidosProveedor");

            migrationBuilder.DropTable(
                name: "PedidosMain");
        }
    }
}
