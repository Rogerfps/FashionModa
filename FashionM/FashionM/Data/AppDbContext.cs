using FashionM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FashionM.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
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

        public DbSet<ImagenZapato> ImagenesZapato { get; set; }

        public DbSet<PedidoMain> PedidosMain { get; set; }

        public DbSet<PedidoProveedor> PedidosProveedor { get; set; }

        public DbSet<PedidoProveedorDetalle> PedidosProveedorDetalle { get; set; }

        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }

        public DbSet<MovimientoDetalle> MovimientosDetalle { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Inventario -> Fotos
            modelBuilder.Entity<Foto>()
                .HasOne(f => f.Inventario)
                .WithMany(i => i.Fotos)
                .HasForeignKey(f => f.InventarioCodigo)
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


            modelBuilder.Entity<PedidoClienteDetalle>()
                .HasOne(d => d.Proveedor)
                .WithMany()
                .HasForeignKey(d => d.ProveedorCedula)
                .HasPrincipalKey(p => p.Cedula)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Proveedor>()
                .HasMany(p => p.Zapatos)
                .WithOne(z => z.Proveedor)
                .HasForeignKey(z => z.ProveedorCedula)
                .OnDelete(DeleteBehavior.Cascade);

            // Zapato -> Imagenes
            modelBuilder.Entity<ImagenZapato>()
                .HasOne(i => i.Zapato)
                .WithMany(z => z.Imagenes)
                .HasForeignKey(i => i.ZapatoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PedidoProveedor>()
                .HasOne<PedidoMain>()
                .WithMany(p => p.PedidosProveedor)
                .HasForeignKey(p => p.PedidoMainId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PedidoProveedor>()
                .HasOne(p => p.Proveedor)
                .WithMany()
                .HasForeignKey(p => p.ProveedorCedula)
                .HasPrincipalKey(p => p.Cedula)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Inventario)
                .WithMany()
                .HasForeignKey(m => m.InventarioCodigo);

            modelBuilder.Entity<MovimientoDetalle>()
                .HasOne(d => d.MovimientoInventario)
                .WithMany(m => m.Detalles)
                .HasForeignKey(d => d.MovimientoInventarioId);
        }
    }
}
