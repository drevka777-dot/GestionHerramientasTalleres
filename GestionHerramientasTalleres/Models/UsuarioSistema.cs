
using Microsoft.AspNetCore.Identity;

namespace GestionHerramientasTalleres.Models
{
    public class UsuarioSistema : IdentityUser
    {
        public string NombreCompleto { get; set; } = "";
    }
}
