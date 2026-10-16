using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace qrs_back.Api.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false, collation: "NOCASE"),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false, collation: "NOCASE"),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Procesos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false, collation: "NOCASE"),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Procesos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false, collation: "NOCASE"),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Producciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Op = table.Column<long>(type: "INTEGER", nullable: false),
                    Modelo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletadaEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EliminadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producciones", x => x.Id);
                    table.CheckConstraint("CK_Produccion_Op", "Op > 0");
                    table.ForeignKey(
                        name: "FK_Producciones_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Producciones_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ordenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProduccionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Np = table.Column<long>(type: "INTEGER", nullable: false),
                    ProcesoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProveedorId = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecioCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    PrecioSegundaCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    Comentario = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Token = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    TokenCreadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecibidoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CitaPropuesta = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CitaConfirmada = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EliminadoEn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ordenes", x => x.Id);
                    table.CheckConstraint("CK_Orden_Np", "Np > 0");
                    table.CheckConstraint("CK_Orden_Precios", "PrecioCentavos >= 0 AND PrecioSegundaCentavos >= 0");
                    table.ForeignKey(
                        name: "FK_Ordenes_Procesos_ProcesoId",
                        column: x => x.ProcesoId,
                        principalTable: "Procesos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ordenes_Producciones_ProduccionId",
                        column: x => x.ProduccionId,
                        principalTable: "Producciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ordenes_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LotesSegundas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrdenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecioCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesSegundas", x => x.Id);
                    table.CheckConstraint("CK_LoteSegundas", "Cantidad > 0 AND PrecioCentavos >= 0");
                    table.ForeignKey(
                        name: "FK_LotesSegundas_Ordenes_OrdenId",
                        column: x => x.OrdenId,
                        principalTable: "Ordenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdenTallas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrdenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Talla = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Posicion = table.Column<int>(type: "INTEGER", nullable: false),
                    Pedidas = table.Column<int>(type: "INTEGER", nullable: false),
                    Primeras = table.Column<int>(type: "INTEGER", nullable: false),
                    Segundas = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenTallas", x => x.Id);
                    table.CheckConstraint("CK_OrdenTalla_Cantidades", "Pedidas >= 0 AND Primeras >= 0 AND Segundas >= 0");
                    table.ForeignKey(
                        name: "FK_OrdenTallas_Ordenes_OrdenId",
                        column: x => x.OrdenId,
                        principalTable: "Ordenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Empresas",
                columns: new[] { "Id", "Activo", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "American Polo" },
                    { 2, true, "Cartoonia" },
                    { 3, true, "Cotton & Print" },
                    { 4, true, "MJCO" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Nombre",
                table: "Clientes",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Nombre",
                table: "Empresas",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LotesSegundas_OrdenId",
                table: "LotesSegundas",
                column: "OrdenId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_CitaConfirmada",
                table: "Ordenes",
                column: "CitaConfirmada");

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_Np",
                table: "Ordenes",
                column: "Np");

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_ProcesoId",
                table: "Ordenes",
                column: "ProcesoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_ProduccionId",
                table: "Ordenes",
                column: "ProduccionId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_ProveedorId",
                table: "Ordenes",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_Token",
                table: "Ordenes",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenTallas_OrdenId_Talla",
                table: "OrdenTallas",
                columns: new[] { "OrdenId", "Talla" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procesos_Nombre",
                table: "Procesos",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producciones_ClienteId",
                table: "Producciones",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Producciones_EmpresaId_Op",
                table: "Producciones",
                columns: new[] { "EmpresaId", "Op" },
                unique: true,
                filter: "EliminadoEn IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Producciones_Modelo",
                table: "Producciones",
                column: "Modelo");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Nombre",
                table: "Proveedores",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LotesSegundas");

            migrationBuilder.DropTable(
                name: "OrdenTallas");

            migrationBuilder.DropTable(
                name: "Ordenes");

            migrationBuilder.DropTable(
                name: "Procesos");

            migrationBuilder.DropTable(
                name: "Producciones");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Empresas");
        }
    }
}
