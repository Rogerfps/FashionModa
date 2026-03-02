using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class Proveedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Codigos",
                columns: table => new
                {
                    IdCodigo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreCodigo = table.Column<string>(type: "text", nullable: false),
                    ProveedorCedula = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Codigos", x => x.IdCodigo);
                    table.ForeignKey(
                        name: "FK_Codigos_Proveedores_ProveedorCedula",
                        column: x => x.ProveedorCedula,
                        principalTable: "Proveedores",
                        principalColumn: "Cedula",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Colores",
                columns: table => new
                {
                    IdColor = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreColor = table.Column<string>(type: "text", nullable: false),
                    CodigoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colores", x => x.IdColor);
                    table.ForeignKey(
                        name: "FK_Colores_Codigos_CodigoId",
                        column: x => x.CodigoId,
                        principalTable: "Codigos",
                        principalColumn: "IdCodigo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Suelas",
                columns: table => new
                {
                    IdSuela = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoSuela = table.Column<string>(type: "text", nullable: false),
                    ColorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suelas", x => x.IdSuela);
                    table.ForeignKey(
                        name: "FK_Suelas_Colores_ColorId",
                        column: x => x.ColorId,
                        principalTable: "Colores",
                        principalColumn: "IdColor",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Codigos_ProveedorCedula",
                table: "Codigos",
                column: "ProveedorCedula");

            migrationBuilder.CreateIndex(
                name: "IX_Colores_CodigoId",
                table: "Colores",
                column: "CodigoId");

            migrationBuilder.CreateIndex(
                name: "IX_Suelas_ColorId",
                table: "Suelas",
                column: "ColorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Suelas");

            migrationBuilder.DropTable(
                name: "Colores");

            migrationBuilder.DropTable(
                name: "Codigos");
        }
    }
}
