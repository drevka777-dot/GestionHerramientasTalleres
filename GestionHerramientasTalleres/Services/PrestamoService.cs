using System.Security.Claims;
using GestionHerramientasTalleres.Data;
using GestionHerramientasTalleres.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GestionHerramientasTalleres.Services
{
    public class PrestamoService
    {
        private readonly IDbContextFactory<AppDbContext> dbFactory;
        private readonly AuthenticationStateProvider authProvider;

        public PrestamoService(
            IDbContextFactory<AppDbContext> dbFactory,
            AuthenticationStateProvider authProvider)
        {
            this.dbFactory = dbFactory;
            this.authProvider = authProvider;
        }

        // Obtener la cuenta que tiene la sesión iniciada.
        private async Task<ClaimsPrincipal> ObtenerUsuarioAsync()
        {
            var estado = await authProvider
                .GetAuthenticationStateAsync();

            var usuario = estado.User;

            if (usuario.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException(
                    "Debes iniciar sesión.");
            }

            return usuario;
        }

        // Consultar únicamente las solicitudes del usuario actual.
        public async Task<List<Prestamo>> ObtenerMisPrestamosAsync()
        {
            var usuario = await ObtenerUsuarioAsync();

            var usuarioId = usuario.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario.");
            }

            await using var db =
                await dbFactory.CreateDbContextAsync();

            return await db.Prestamos
                .AsNoTracking()
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.FechaSolicitud)
                .ToListAsync();
        }

        // Registrar una solicitud sin cambiar la disponibilidad.
        public async Task<string> SolicitarPrestamoAsync(
            string herramientaCodigo)
        {
            var usuario = await ObtenerUsuarioAsync();

            if (!usuario.IsInRole("Usuario"))
            {
                throw new UnauthorizedAccessException(
                    "Esta operación requiere el rol Usuario.");
            }

            var usuarioId = usuario.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario.");
            }

            herramientaCodigo = herramientaCodigo?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(herramientaCodigo))
            {
                return "Selecciona una herramienta.";
            }

            await using var db =
                await dbFactory.CreateDbContextAsync();

            await using var transaccion =
                await db.Database.BeginTransactionAsync();

            var herramienta = await db.Herramientas
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    h => h.Codigo == herramientaCodigo);

            if (herramienta == null)
            {
                return "La herramienta no existe.";
            }

            bool tienePrestamoActivo = await db.Prestamos
                .AnyAsync(p =>
                    p.HerramientaCodigo == herramientaCodigo &&
                    p.Estado == EstadoPrestamo.Aprobado);

            if (!herramienta.Disponible || tienePrestamoActivo)
            {
                return "La herramienta no está disponible.";
            }

            bool tieneSolicitudPendiente = await db.Prestamos
                .AnyAsync(p =>
                    p.HerramientaCodigo == herramientaCodigo &&
                    p.UsuarioId == usuarioId &&
                    p.Estado == EstadoPrestamo.Pendiente);

            if (tieneSolicitudPendiente)
            {
                return "Ya tienes una solicitud pendiente para esta herramienta.";
            }

            var prestamo = new Prestamo
            {
                HerramientaCodigo = herramienta.Codigo,
                UsuarioId = usuarioId,
                Estado = EstadoPrestamo.Pendiente,
                FechaSolicitud = DateTime.UtcNow
            };

            db.Prestamos.Add(prestamo);

            await db.SaveChangesAsync();
            await transaccion.CommitAsync();

            return "Solicitud registrada. Está pendiente de aprobación.";
        }
        // Verificar que la sesión corresponde a un administrador.
        private async Task<string> ObtenerAdministradorIdAsync()
        {
            var usuario = await ObtenerUsuarioAsync();

            if (!usuario.IsInRole("Administrador"))
            {
                throw new UnauthorizedAccessException(
                    "Esta operación requiere el rol Administrador.");
            }

            var administradorId = usuario.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(administradorId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al administrador.");
            }

            return administradorId;
        }

        // Consultar las solicitudes de todos los usuarios.
        public async Task<List<Prestamo>> ObtenerTodosLosPrestamosAsync()
        {
            await ObtenerAdministradorIdAsync();

            await using var db =
                await dbFactory.CreateDbContextAsync();

            return await db.Prestamos
                .AsNoTracking()
                .OrderByDescending(p => p.FechaSolicitud)
                .ToListAsync();
        }

        // Aprobar y actualizar la herramienta en una misma transacción.
        public async Task<string> AprobarPrestamoAsync(int prestamoId)
        {
            var administradorId =
                await ObtenerAdministradorIdAsync();

            await using var db =
                await dbFactory.CreateDbContextAsync();

            await using var transaccion =
                await db.Database.BeginTransactionAsync();

            var prestamo = await db.Prestamos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == prestamoId);

            if (prestamo == null)
            {
                return "La solicitud no existe.";
            }

            if (prestamo.Estado != EstadoPrestamo.Pendiente)
            {
                return "Solo se pueden aprobar solicitudes pendientes.";
            }

            bool existePrestamoActivo = await db.Prestamos
                .AnyAsync(p =>
                    p.HerramientaCodigo == prestamo.HerramientaCodigo &&
                    p.Estado == EstadoPrestamo.Aprobado);

            if (existePrestamoActivo)
            {
                return "La herramienta ya tiene un préstamo aprobado.";
            }

            // Cambiar la disponibilidad solo si sigue disponible.
            int herramientasActualizadas = await db.Herramientas
                .Where(h =>
                    h.Codigo == prestamo.HerramientaCodigo &&
                    h.Disponible)
                .ExecuteUpdateAsync(actualizacion => actualizacion
                    .SetProperty(h => h.Disponible, false));

            if (herramientasActualizadas != 1)
            {
                return "La herramienta ya no está disponible.";
            }

            var fecha = DateTime.UtcNow;

            // Confirmar solo si la solicitud continúa pendiente.
            int prestamosActualizados = await db.Prestamos
                .Where(p =>
                    p.Id == prestamoId &&
                    p.Estado == EstadoPrestamo.Pendiente)
                .ExecuteUpdateAsync(actualizacion => actualizacion
                    .SetProperty(
                        p => p.Estado,
                        EstadoPrestamo.Aprobado)
                    .SetProperty(
                        p => p.FechaResolucion,
                        (DateTime?)fecha)
                    .SetProperty(
                        p => p.AdministradorResolucionId,
                        administradorId));

            if (prestamosActualizados != 1)
            {
                // Sin Commit, se revierte también la disponibilidad.
                return "La solicitud cambió. Actualiza la lista.";
            }

            await transaccion.CommitAsync();

            return "Préstamo aprobado. La herramienta quedó prestada.";
        }

        // Rechazar sin modificar la disponibilidad.
        public async Task<string> RechazarPrestamoAsync(int prestamoId)
        {
            var administradorId =
                await ObtenerAdministradorIdAsync();

            await using var db =
                await dbFactory.CreateDbContextAsync();

            var fecha = DateTime.UtcNow;

            int prestamosActualizados = await db.Prestamos
                .Where(p =>
                    p.Id == prestamoId &&
                    p.Estado == EstadoPrestamo.Pendiente)
                .ExecuteUpdateAsync(actualizacion => actualizacion
                    .SetProperty(
                        p => p.Estado,
                        EstadoPrestamo.Rechazado)
                    .SetProperty(
                        p => p.FechaResolucion,
                        (DateTime?)fecha)
                    .SetProperty(
                        p => p.AdministradorResolucionId,
                        administradorId));

            if (prestamosActualizados != 1)
            {
                return "La solicitud no existe o ya fue procesada.";
            }

            return "Solicitud rechazada.";
        }
        // Registrar la devolución y actualizar el inventario.
        public async Task<string> RegistrarDevolucionAsync(int prestamoId)
        {
            var administradorId =
                await ObtenerAdministradorIdAsync();

            await using var db =
                await dbFactory.CreateDbContextAsync();

            await using var transaccion =
                await db.Database.BeginTransactionAsync();

            var prestamo = await db.Prestamos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == prestamoId);

            if (prestamo == null)
            {
                return "El préstamo no existe.";
            }

            if (prestamo.Estado != EstadoPrestamo.Aprobado)
            {
                return "Solo se pueden devolver préstamos aprobados.";
            }

            var fecha = DateTime.UtcNow;

            int prestamosActualizados = await db.Prestamos
                .Where(p =>
                    p.Id == prestamoId &&
                    p.Estado == EstadoPrestamo.Aprobado)
                .ExecuteUpdateAsync(actualizacion => actualizacion
                    .SetProperty(
                        p => p.Estado,
                        EstadoPrestamo.Devuelto)
                    .SetProperty(
                        p => p.FechaDevolucion,
                        (DateTime?)fecha)
                    .SetProperty(
                        p => p.AdministradorDevolucionId,
                        administradorId));

            if (prestamosActualizados != 1)
            {
                return "El préstamo ya fue procesado. Actualiza la lista.";
            }

            bool otroPrestamoActivo = await db.Prestamos
                .AnyAsync(p =>
                    p.HerramientaCodigo == prestamo.HerramientaCodigo &&
                    p.Estado == EstadoPrestamo.Aprobado);

            int herramientasActualizadas = await db.Herramientas
                .Where(h => h.Codigo == prestamo.HerramientaCodigo)
                .ExecuteUpdateAsync(actualizacion => actualizacion
                    .SetProperty(
                        h => h.Disponible,
                        !otroPrestamoActivo));

            if (herramientasActualizadas != 1)
            {
                // Sin Commit, se revierte también la devolución.
                return "No se encontró la herramienta del préstamo.";
            }

            await transaccion.CommitAsync();

            return otroPrestamoActivo
                ? "Devolución registrada. La herramienta conserva otro préstamo activo."
                : "Devolución registrada. La herramienta está disponible.";
        }

    }
}