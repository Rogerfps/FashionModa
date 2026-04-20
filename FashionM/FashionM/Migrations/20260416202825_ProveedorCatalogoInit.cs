using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class ProveedorCatalogoInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProveedoresCatalogo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Cedula = table.Column<string>(type: "text", nullable: false),
                    Telefonos = table.Column<string>(type: "text", nullable: false),
                    Direccion = table.Column<string>(type: "text", nullable: false),
                    Correo = table.Column<string>(type: "text", nullable: false),
                    ActividadEconomica = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProveedoresCatalogo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZapatosProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImagenUrl = table.Column<string>(type: "text", nullable: true),
                    ProveedorCatalogoId = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZapatosProveedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZapatosProveedor_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZapatosProveedor_ProveedoresCatalogo_ProveedorCatalogoId",
                        column: x => x.ProveedorCatalogoId,
                        principalTable: "ProveedoresCatalogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VariantesZapatoProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Suela = table.Column<string>(type: "text", nullable: false),
                    Detalle = table.Column<string>(type: "text", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "numeric", nullable: false),
                    PrecioCosto = table.Column<decimal>(type: "numeric", nullable: false),
                    CostoCOP = table.Column<decimal>(type: "numeric", nullable: false),
                    ZapatoProveedorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantesZapatoProveedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariantesZapatoProveedor_ZapatosProveedor_ZapatoProveedorId",
                        column: x => x.ZapatoProveedorId,
                        principalTable: "ZapatosProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TallasVarianteZapatoProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric", nullable: true),
                    VarianteZapatoProveedorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TallasVarianteZapatoProveedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TallasVarianteZapatoProveedor_VariantesZapatoProveedor_Vari~",
                        column: x => x.VarianteZapatoProveedorId,
                        principalTable: "VariantesZapatoProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TallasVarianteZapatoProveedor_VarianteZapatoProveedorId",
                table: "TallasVarianteZapatoProveedor",
                column: "VarianteZapatoProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_VariantesZapatoProveedor_ZapatoProveedorId",
                table: "VariantesZapatoProveedor",
                column: "ZapatoProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_ZapatosProveedor_EmpresaId",
                table: "ZapatosProveedor",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ZapatosProveedor_ProveedorCatalogoId",
                table: "ZapatosProveedor",
                column: "ProveedorCatalogoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TallasVarianteZapatoProveedor");

            migrationBuilder.DropTable(
                name: "VariantesZapatoProveedor");

            migrationBuilder.DropTable(
                name: "ZapatosProveedor");

            migrationBuilder.DropTable(
                name: "ProveedoresCatalogo");
        }
    }
}
