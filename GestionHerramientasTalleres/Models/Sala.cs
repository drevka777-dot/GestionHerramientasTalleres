
using System.ComponentModel.DataAnnotations;

namespace GestionHerramientasTalleres.Models
{
    public class Sala
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la sala es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El tipo de sala es obligatorio.")]
        [StringLength(50)]
        public string Tipo { get; set; } = "";

        [StringLength(150)]
        public string Ubicacion { get; set; } = "";

        [StringLength(300)]
        public string Descripcion { get; set; } = "";

        public bool Activa { get; set; } = true;
    }
}
