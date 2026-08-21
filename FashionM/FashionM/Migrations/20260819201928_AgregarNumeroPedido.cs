using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AgregarNumeroPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumeroPedido",
                table: "PedidosCliente",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroPedido",
                table: "PedidosCliente");
        }
    }
}
