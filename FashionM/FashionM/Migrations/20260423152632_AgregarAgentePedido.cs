using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAgentePedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Agente",
                table: "PedidosCliente",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Agente",
                table: "PedidosCliente");
        }
    }
}
