using FashionM.Models;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Inventario> Inventarios { get; set; }
        public DbSet<Foto> Fotos { get; set; }
        public DbSet<TallaInventario> TallasInventario { get; set; }
        public DbSet<Clientes> Clientes { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Zapato> Zapatos { get; set; }

        public DbSet<PedidoCliente> PedidosCliente { get; set; }
        public DbSet<PedidoClienteDetalle> PedidoClienteDetalles { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Inventario -> Fotos
            modelBuilder.Entity<Foto>()
                .HasOne(f => f.Inventario)
                .WithMany(i => i.Fotos)
                .HasForeignKey(f => f.InventarioId)
                .OnDelete(DeleteBehavior.Cascade);

            //  Inventario -> Tallas 
            modelBuilder.Entity<TallaInventario>()
                .HasOne(t => t.Inventario)
                .WithMany(i => i.Tallas)
                .HasForeignKey(t => t.InventarioCodigo)
                .OnDelete(DeleteBehavior.Cascade);

            // Cliente -> Pedidos
            modelBuilder.Entity<PedidoCliente>()
                .HasOne(p => p.Cliente)
                .WithMany(c => c.Pedidos)
                .HasForeignKey(p => p.ClienteCedula)
                .OnDelete(DeleteBehavior.Restrict);

            // Pedido -> Detalles
            modelBuilder.Entity<PedidoClienteDetalle>()
                .HasOne(d => d.PedidoCliente)
                .WithMany(p => p.Detalles)
                .HasForeignKey(d => d.PedidoClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Proveedor>()
                .HasMany(p => p.Zapatos)
                .WithOne(z => z.Proveedor)
                .HasForeignKey(z => z.ProveedorCedula)
                .OnDelete(DeleteBehavior.Cascade);


        }
    }
}