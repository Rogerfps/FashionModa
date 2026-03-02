using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class EstadoCreditoPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirmaCredito",
                table: "PedidosCliente");

            migrationBuilder.DropColumn(
                name: "Vendedor",
                table: "PedidosCliente");

            migrationBuilder.AddColumn<int>(
                name: "EstadoCredito",
                table: "PedidosCliente",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoCredito",
                table: "PedidosCliente");

            migrationBuilder.AddColumn<bool>(
                name: "FirmaCredito",
                table: "PedidosCliente",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Vendedor",
                table: "PedidosCliente",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
