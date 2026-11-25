
using GestionHerramientasTalleres.Data;
using GestionHerramientasTalleres.Models;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GestionHerramientasTalleres.Services
{
    public class SalaService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        private readonly AuthenticationStateProvider
            _authenticationStateProvider;

        public SalaService(
            IDbContextFactory<AppDbContext> factory,
            AuthenticationStateProvider authenticationStateProvider)
        {
            _factory = factory;

            _authenticationStateProvider =
                authenticationStateProvider;
        }

        // ========================================
        // COMPROBAR PERMISOS
        // ========================================

        private async Task VerificarAdministradorAsync()
        {
            var estado = await _authenticationStateProvider
                .GetAuthenticationStateAsync();

            if (estado.User.Identity?.IsAuthenticated != true ||
                !estado.User.IsInRole("Administrador"))
            {
                throw new UnauthorizedAccessException(
                    "Solo un administrador puede modificar salas.");
            }
        }

        // ========================================
        // CONSULTAR SALAS
        // ========================================

        public List<Sala> ObtenerSalas()
        {
            using var db = _factory.CreateDbContext();

            return db.Salas
                .AsNoTracking()
                .OrderBy(s => s.Nombre)
                .ToList();
        }

        // ========================================
        // REGISTRAR SALA
        // ========================================

        public async Task<bool> RegistrarSalaAsync(
            string nombre,
            string tipo,
            string ubicacion,
            string descripcion)
        {
            await VerificarAdministradorAsync();

            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(tipo))
            {
                return false;
            }

            using var db = _factory.CreateDbContext();

            var sala = new Sala
            {
                Nombre = nombre.Trim(),
                Tipo = tipo.Trim(),
                Ubicacion = (ubicacion ?? "").Trim(),
                Descripcion = (descripcion ?? "").Trim(),
                Activa = true
            };

            db.Salas.Add(sala);

            await db.SaveChangesAsync();

            return true;
        }

        // ========================================
        // EDITAR SALA
        // ========================================

        public async Task<bool> EditarSalaAsync(
            int id,
            string nombre,
            string tipo,
            string ubicacion,
            string descripcion)
        {
            await VerificarAdministradorAsync();

            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(tipo))
            {
                return false;
            }

            using var db = _factory.CreateDbContext();

            var sala = await db.Salas.FindAsync(id);

            if (sala is null)
            {
                return false;
            }

            sala.Nombre = nombre.Trim();
            sala.Tipo = tipo.Trim();
            sala.Ubicacion = (ubicacion ?? "").Trim();
            sala.Descripcion = (descripcion ?? "").Trim();

            await db.SaveChangesAsync();

            return true;
        }

        // ========================================
        // ACTIVAR O DESACTIVAR SALA
        // ========================================

        public async Task<bool> CambiarEstadoAsync(int id)
        {
            await VerificarAdministradorAsync();

            using var db = _factory.CreateDbContext();

            var sala = await db.Salas.FindAsync(id);

            if (sala is null)
            {
                return false;
            }

            sala.Activa = !sala.Activa;

            await db.SaveChangesAsync();

            return true;
        }
    }
}
