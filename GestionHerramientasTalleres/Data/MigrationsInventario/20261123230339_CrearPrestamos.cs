using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionHerramientasTalleres.Data.MigrationsInventario
{
    /// <inheritdoc />
    public partial class CrearPrestamos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prestamos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HerramientaCodigo = table.Column<string>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AdministradorResolucionId = table.Column<string>(type: "TEXT", nullable: true),
                    FechaDevolucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AdministradorDevolucionId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestamos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prestamos_Herramientas_HerramientaCodigo",
                        column: x => x.HerramientaCodigo,
                        principalTable: "Herramientas",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_HerramientaCodigo",
                table: "Prestamos",
                column: "HerramientaCodigo",
                unique: true,
                filter: "\"Estado\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_UsuarioId",
                table: "Prestamos",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Prestamos");
        }
    }
}
