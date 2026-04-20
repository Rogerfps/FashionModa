using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionM.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEmpresaAZapato : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ⚠️ No hacemos nada aquí porque:
            // - La columna Empresa ya existe en Zapatos
            // - Ya no existe en Proveedores
            // - No hay datos que migrar

            // 🔥 Migración vacía para mantener historial consistente
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ⚠️ Tampoco revertimos porque no sabemos estado original real
        }
    }
}