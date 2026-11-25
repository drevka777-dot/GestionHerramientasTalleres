
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionHerramientasTalleres.Data.MigrationsInventario
{
    public partial class CrearSalas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La tabla Herramientas ya existe.
            // Solo creamos la nueva tabla Salas.

            migrationBuilder.CreateTable(
                name: "Salas",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "INTEGER",
                        nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),

                    Nombre = table.Column<string>(
                        type: "TEXT",
                        maxLength: 100,
                        nullable: false),

                    Tipo = table.Column<string>(
                        type: "TEXT",
                        maxLength: 50,
                        nullable: false),

                    Ubicacion = table.Column<string>(
                        type: "TEXT",
                        maxLength: 150,
                        nullable: false),

                    Descripcion = table.Column<string>(
                        type: "TEXT",
                        maxLength: 300,
                        nullable: false),

                    Activa = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Salas",
                        x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir esta migración elimina únicamente Salas.
            // Nunca eliminamos Herramientas.

            migrationBuilder.DropTable(
                name: "Salas");
        }
    }
}
