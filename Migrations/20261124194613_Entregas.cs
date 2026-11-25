using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qrs_back.Api.Migrations
{
    /// <inheritdoc />
    public partial class Entregas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeclaradoJson",
                table: "Ordenes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EntregadaEn",
                table: "Ordenes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotaEntrega",
                table: "Ordenes",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PedidoCorregidoEn",
                table: "Ordenes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecibioNombre",
                table: "Ordenes",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CambiosOrden",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrdenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Accion = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Quien = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Detalle = table.Column<string>(type: "TEXT", nullable: true),
                    RegistradoEn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CambiosOrden", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CambiosOrden_OrdenId",
                table: "CambiosOrden",
                column: "OrdenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CambiosOrden");

            migrationBuilder.DropColumn(
                name: "DeclaradoJson",
                table: "Ordenes");

            migrationBuilder.DropColumn(
                name: "EntregadaEn",
                table: "Ordenes");

            migrationBuilder.DropColumn(
                name: "NotaEntrega",
                table: "Ordenes");

            migrationBuilder.DropColumn(
                name: "PedidoCorregidoEn",
                table: "Ordenes");

            migrationBuilder.DropColumn(
                name: "RecibioNombre",
                table: "Ordenes");
        }
    }
}
