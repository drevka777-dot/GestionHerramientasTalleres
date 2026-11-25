using GestionHerramientasTalleres.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionHerramientasTalleres.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(
            DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // INVENTARIO
        public DbSet<Herramienta> Herramientas
            => Set<Herramienta>();

        // SALAS Y LABORATORIOS
        public DbSet<Sala> Salas
            => Set<Sala>();

        // PRÉSTAMOS
        public DbSet<Prestamo> Prestamos
            => Set<Prestamo>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // CONFIGURACIÓN DE HERRAMIENTAS
            modelBuilder.Entity<Herramienta>(entity =>
            {
                entity.HasKey(h => h.Codigo);

                entity.HasOne<Sala>()
                    .WithMany()
                    .HasForeignKey(h => h.SalaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(h => h.SalaId);
            });

            // CONFIGURACIÓN DE SALAS
            modelBuilder.Entity<Sala>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.Nombre)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(s => s.Tipo)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(s => s.Ubicacion)
                    .HasMaxLength(150);

                entity.Property(s => s.Descripcion)
                    .HasMaxLength(300);

                entity.Property(s => s.Activa)
                    .HasDefaultValue(true);
            });

            // CONFIGURACIÓN DE PRÉSTAMOS
            modelBuilder.Entity<Prestamo>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.HerramientaCodigo)
                    .IsRequired();

                entity.Property(p => p.UsuarioId)
                    .IsRequired();

                entity.Property(p => p.Estado)
                    .HasConversion<int>();

                // Utiliza la herramienta del inventario existente.
                entity.HasOne<Herramienta>()
                    .WithMany()
                    .HasForeignKey(p => p.HerramientaCodigo)
                    .OnDelete(DeleteBehavior.Restrict);

                // Consultar solicitudes por usuario.
                entity.HasIndex(p => p.UsuarioId);

                // Solo un préstamo aprobado por herramienta.
                // Aprobado = 1 en EstadoPrestamo.
                entity.HasIndex(p => p.HerramientaCodigo)
                    .IsUnique()
                    .HasFilter("\"Estado\" = 1");
            });
        }
    }
}