using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class PermitirEliminarInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProformaDetalles_Inventarios_InventarioCodigo",
                table: "ProformaDetalles");

            migrationBuilder.AlterColumn<string>(
                name: "InventarioCodigo",
                table: "ProformaDetalles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_ProformaDetalles_Inventarios_InventarioCodigo",
                table: "ProformaDetalles",
                column: "InventarioCodigo",
                principalTable: "Inventarios",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProformaDetalles_Inventarios_InventarioCodigo",
                table: "ProformaDetalles");

            migrationBuilder.AlterColumn<string>(
                name: "InventarioCodigo",
                table: "ProformaDetalles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProformaDetalles_Inventarios_InventarioCodigo",
                table: "ProformaDetalles",
                column: "InventarioCodigo",
                principalTable: "Inventarios",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
