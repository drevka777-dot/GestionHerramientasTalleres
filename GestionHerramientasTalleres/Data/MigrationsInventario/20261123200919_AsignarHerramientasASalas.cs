using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionHerramientasTalleres.Data.MigrationsInventario
{
    /// <inheritdoc />
    public partial class AsignarHerramientasASalas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SalaId",
                table: "Herramientas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Herramientas_SalaId",
                table: "Herramientas",
                column: "SalaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Herramientas_Salas_SalaId",
                table: "Herramientas",
                column: "SalaId",
                principalTable: "Salas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Herramientas_Salas_SalaId",
                table: "Herramientas");

            migrationBuilder.DropIndex(
                name: "IX_Herramientas_SalaId",
                table: "Herramientas");

            migrationBuilder.DropColumn(
                name: "SalaId",
                table: "Herramientas");
        }
    }
}
