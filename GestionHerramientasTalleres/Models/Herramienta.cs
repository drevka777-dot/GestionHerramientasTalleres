
namespace GestionHerramientasTalleres.Models
{
    public class Herramienta
    {
        // Código único de la herramienta
        public string Codigo { get; set; } = "";

        // Nombre de la herramienta
        public string Nombre { get; set; } = "";

        // Disponibilidad
        public bool Disponible { get; set; } = true;

        // Sala a la que pertenece
        // Null significa que todavía no está asignada
        public int? SalaId { get; set; }
    }
}
