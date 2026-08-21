using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVendedorYTotalParesPedidoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalPares",
                table: "PedidosCliente",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Vendedor",
                table: "PedidosCliente",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPares",
                table: "PedidosCliente");

            migrationBuilder.DropColumn(
                name: "Vendedor",
                table: "PedidosCliente");
        }
    }
}
