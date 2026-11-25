
using GestionHerramientasTalleres.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionHerramientasTalleres.Data
{
    public class UsuariosDbContext
        : IdentityDbContext<UsuarioSistema>
    {
        public UsuariosDbContext(
            DbContextOptions<UsuariosDbContext> options)
            : base(options)
        {
        }
    }
}
