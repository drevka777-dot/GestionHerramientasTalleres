using System.ComponentModel.DataAnnotations;

namespace GestionHerramientasTalleres.Models
{
    public enum EstadoPrestamo
    {
        Pendiente = 0,
        Aprobado = 1,
        Rechazado = 2,
        Devuelto = 3
    }

    public class Prestamo
    {
        public int Id { get; set; }

        [Required]
        public string HerramientaCodigo { get; set; } = "";

        [Required]
        public string UsuarioId { get; set; } = "";

        public EstadoPrestamo Estado { get; set; }
            = EstadoPrestamo.Pendiente;

        public DateTime FechaSolicitud { get; set; }
            = DateTime.UtcNow;

        public DateTime? FechaResolucion { get; set; }

        public string? AdministradorResolucionId { get; set; }

        public DateTime? FechaDevolucion { get; set; }

        public string? AdministradorDevolucionId { get; set; }
    }
}