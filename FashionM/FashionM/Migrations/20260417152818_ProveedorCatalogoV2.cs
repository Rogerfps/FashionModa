using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class ProveedorCatalogoV2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ZapatosProveedor_Empresas_EmpresaId",
                table: "ZapatosProveedor");

            migrationBuilder.DropTable(name: "TallasVarianteZapatoProveedor");
            migrationBuilder.DropTable(name: "VariantesZapatoProveedor");

            migrationBuilder.DropIndex(
                name: "IX_ZapatosProveedor_EmpresaId",
                table: "ZapatosProveedor");

            // 🔥 1. Crear nueva columna Empresa
            migrationBuilder.AddColumn<string>(
                name: "Empresa",
                table: "ZapatosProveedor",
                type: "text",
                nullable: false,
                defaultValue: "");

            // 🔥 2. Migrar datos (int → string)
            migrationBuilder.Sql(@"
            UPDATE ""ZapatosProveedor""
            SET ""Empresa"" = ""EmpresaId""::text
        ");

            // 🔥 3. Eliminar columna vieja
            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "ZapatosProveedor");

            // ===============================
            // NUEVAS COLUMNAS
            // ===============================
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioColombia",
                table: "ZapatosProveedor",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioCosto",
                table: "ZapatosProveedor",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioVenta",
                table: "ZapatosProveedor",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            // ===============================
            // NUEVAS TABLAS
            // ===============================
            migrationBuilder.CreateTable(
                name: "ColoresZapato",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    ZapatoProveedorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColoresZapato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColoresZapato_ZapatosProveedor_ZapatoProveedorId",
                        column: x => x.ZapatoProveedorId,
                        principalTable: "ZapatosProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetallesZapato",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    ZapatoProveedorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesZapato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesZapato_ZapatosProveedor_ZapatoProveedorId",
                        column: x => x.ZapatoProveedorId,
                        principalTable: "ZapatosProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuelasZapato",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    ZapatoProveedorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuelasZapato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuelasZapato_ZapatosProveedor_ZapatoProveedorId",
                        column: x => x.ZapatoProveedorId,
                        principalTable: "ZapatosProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TallasZapato",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric", nullable: true),
                    ZapatoProveedorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TallasZapato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TallasZapato_ZapatosProveedor_ZapatoProveedorId",
                        column: x => x.ZapatoProveedorId,
                        principalTable: "ZapatosProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ===============================
            // INDEXES
            // ===============================
            migrationBuilder.CreateIndex(
                name: "IX_ColoresZapato_ZapatoProveedorId",
                table: "ColoresZapato",
                column: "ZapatoProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesZapato_ZapatoProveedorId",
                table: "DetallesZapato",
                column: "ZapatoProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_SuelasZapato_ZapatoProveedorId",
                table: "SuelasZapato",
                column: "ZapatoProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_TallasZapato_ZapatoProveedorId",
                table: "TallasZapato",
                column: "ZapatoProveedorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // (puedes dejarlo como está o simplificarlo si quieres)
        }
    }
}
