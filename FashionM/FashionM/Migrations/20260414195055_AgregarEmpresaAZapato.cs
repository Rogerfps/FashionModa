using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEmpresaAZapato : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Agregar columna en Zapatos
            migrationBuilder.AddColumn<string>(
                name: "Empresa",
                table: "Zapatos",
                type: "text",
                nullable: false,
                defaultValue: "");

            // 2. COPIAR datos desde Proveedores (✅ PostgreSQL correcto)
            migrationBuilder.Sql(@"
            UPDATE ""Zapatos""
            SET ""Empresa"" = P.""Empresa""
            FROM ""Proveedores"" P
            WHERE ""Zapatos"".""ProveedorCedula"" = P.""Cedula""
        ");

            // 3. Eliminar columna vieja
            migrationBuilder.DropColumn(
                name: "Empresa",
                table: "Proveedores");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Volver a crear en Proveedores
            migrationBuilder.AddColumn<string>(
                name: "Empresa",
                table: "Proveedores",
                type: "text",
                nullable: false,
                defaultValue: "");

            // 2. Restaurar datos (simple)
            migrationBuilder.Sql(@"
            UPDATE ""Proveedores""
            SET ""Empresa"" = Z.""Empresa""
            FROM ""Zapatos"" Z
            WHERE Z.""ProveedorCedula"" = ""Proveedores"".""Cedula""
        ");

            // 3. Eliminar columna en Zapatos
            migrationBuilder.DropColumn(
                name: "Empresa",
                table: "Zapatos");
        }
    }
}