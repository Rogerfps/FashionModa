using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCostosAdicionalesProforma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProformaCostosAdicionales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProformaId = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProformaCostosAdicionales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProformaCostosAdicionales_Proformas_ProformaId",
                        column: x => x.ProformaId,
                        principalTable: "Proformas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProformaCostosAdicionales_ProformaId",
                table: "ProformaCostosAdicionales",
                column: "ProformaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProformaCostosAdicionales");
        }
    }
}
