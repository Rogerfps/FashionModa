using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class CuentasDias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiasCredito",
                table: "CuentasPorCobrar",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiasCredito",
                table: "CuentasPorCobrar");
        }
    }
}
