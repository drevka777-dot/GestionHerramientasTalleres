
using GestionHerramientasTalleres.Data;
using GestionHerramientasTalleres.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GestionHerramientasTalleres.Services
{
    public class InventarioService
    {
        private readonly IDbContextFactory<AppDbContext> dbFactory;
        private readonly AuthenticationStateProvider authProvider;

        public InventarioService(
            IDbContextFactory<AppDbContext> dbFactory,
            AuthenticationStateProvider authProvider)
        {
            this.dbFactory = dbFactory;
            this.authProvider = authProvider;
        }

        // ========================================
        // VERIFICAR PERMISOS DE ADMINISTRADOR
        // ========================================

        private async Task VerificarAdministradorAsync()
        {
            var estado = await authProvider
                .GetAuthenticationStateAsync();

            var usuario = estado.User;

            if (usuario.Identity?.IsAuthenticated != true ||
                !usuario.IsInRole("Administrador"))
            {
                throw new UnauthorizedAccessException(
                    "Solo los administradores pueden asignar herramientas a salas.");
            }
        }

        // ========================================
        // CONSULTAR HERRAMIENTAS
        // ========================================

        public List<Herramienta> ObtenerHerramientas()
        {
            using var db = dbFactory.CreateDbContext();

            return db.Herramientas
                .AsNoTracking()
                .OrderBy(h => h.Codigo)
                .ToList();
        }

        // ========================================
        // REGISTRAR HERRAMIENTA
        // ========================================

        public bool RegistrarHerramienta(
            string codigo,
            string nombre)
        {
            codigo = codigo?.Trim() ?? "";
            nombre = nombre?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(codigo) ||
                string.IsNullOrWhiteSpace(nombre))
            {
                return false;
            }

            using var db = dbFactory.CreateDbContext();

            bool existe = db.Herramientas
                .AsEnumerable()
                .Any(h => h.Codigo.Equals(
                    codigo,
                    StringComparison.OrdinalIgnoreCase));

            if (existe)
            {
                return false;
            }

            db.Herramientas.Add(new Herramienta
            {
                Codigo = codigo,
                Nombre = nombre,
                Disponible = true,
                SalaId = null
            });

            db.SaveChanges();

            return true;
        }

        // ========================================
        // EDITAR HERRAMIENTA
        // ========================================

        public bool EditarHerramienta(
            string codigo,
            string nuevoNombre)
        {
            codigo = codigo?.Trim() ?? "";
            nuevoNombre = nuevoNombre?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(codigo) ||
                string.IsNullOrWhiteSpace(nuevoNombre))
            {
                return false;
            }

            using var db = dbFactory.CreateDbContext();

            var herramienta = db.Herramientas
                .AsEnumerable()
                .FirstOrDefault(h => h.Codigo.Equals(
                    codigo,
                    StringComparison.OrdinalIgnoreCase));

            if (herramienta == null)
            {
                return false;
            }

            herramienta.Nombre = nuevoNombre;

            db.SaveChanges();

            return true;
        }

        // ========================================
        // CONSULTAR TODAS LAS SALAS
        // CORRECCIÓN DEL ERROR CS1061
        // ========================================

        public List<Sala> ObtenerTodasLasSalas()
        {
            using var db = dbFactory.CreateDbContext();

            return db.Salas
                .AsNoTracking()
                .OrderBy(s => s.Nombre)
                .ToList();
        }

        // ========================================
        // CONSULTAR SALAS ACTIVAS
        // ========================================

        public List<Sala> ObtenerSalasActivas()
        {
            using var db = dbFactory.CreateDbContext();

            return db.Salas
                .AsNoTracking()
                .Where(s => s.Activa)
                .OrderBy(s => s.Nombre)
                .ToList();
        }

        // ========================================
        // ASIGNAR HERRAMIENTA A SALA
        // SOLO ADMINISTRADORES
        // ========================================

        public async Task<bool> AsignarHerramientaASalaAsync(
            string codigo,
            int? salaId)
        {
            await VerificarAdministradorAsync();

            codigo = codigo?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return false;
            }

            await using var db =
                await dbFactory.CreateDbContextAsync();

            var herramientas = await db.Herramientas
                .ToListAsync();

            var herramienta = herramientas
                .FirstOrDefault(h => h.Codigo.Equals(
                    codigo,
                    StringComparison.OrdinalIgnoreCase));

            if (herramienta == null)
            {
                return false;
            }

            // Solo se puede asignar a una sala activa.
            // Null permite quitar la asignación.

            if (salaId.HasValue)
            {
                bool salaValida = await db.Salas
                    .AnyAsync(s =>
                        s.Id == salaId.Value &&
                        s.Activa);

                if (!salaValida)
                {
                    return false;
                }
            }

            herramienta.SalaId = salaId;

            await db.SaveChangesAsync();

            return true;
        }

        // ========================================
        // CONSULTAR NOMBRE DE SALA
        // ========================================

        public string ObtenerNombreSala(int? salaId)
        {
            if (!salaId.HasValue)
            {
                return "Sin asignar";
            }

            using var db = dbFactory.CreateDbContext();

            var sala = db.Salas
                .AsNoTracking()
                .FirstOrDefault(s => s.Id == salaId.Value);

            return sala?.Nombre ?? "Sala no encontrada";
        }
    }
}
