using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace qrs_back.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProveedoresYProcesos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProveedorProcesos",
                columns: table => new
                {
                    ProveedorId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcesoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProveedorProcesos", x => new { x.ProveedorId, x.ProcesoId });
                    table.ForeignKey(
                        name: "FK_ProveedorProcesos_Procesos_ProcesoId",
                        column: x => x.ProcesoId,
                        principalTable: "Procesos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProveedorProcesos_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Nombre",
                value: "Aéropostale");

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 10,
                column: "Nombre",
                value: "LOB");

            migrationBuilder.InsertData(
                table: "Procesos",
                columns: new[] { "Id", "Activo", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "Bordado" },
                    { 2, true, "Confección" },
                    { 3, true, "Estampado" },
                    { 4, true, "Piedra" }
                });

            migrationBuilder.InsertData(
                table: "Proveedores",
                columns: new[] { "Id", "Activo", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "Mara" },
                    { 2, true, "Lalo Silverato" },
                    { 3, true, "Pulpo" },
                    { 4, true, "José Onofre" },
                    { 5, true, "Karina" },
                    { 6, true, "Eva" },
                    { 7, true, "Tamayo" },
                    { 8, true, "José Contreras" },
                    { 9, true, "Fabián" },
                    { 10, true, "Lety" },
                    { 11, true, "Celia" },
                    { 12, true, "Manuel" },
                    { 13, true, "Luis" },
                    { 14, true, "Javier" },
                    { 15, true, "Laura" },
                    { 16, true, "María Ahumada" },
                    { 17, true, "Juan Sosa" },
                    { 18, true, "Lulú Arenas" },
                    { 19, true, "Blanca Mendoza" },
                    { 20, true, "Alejandro" },
                    { 21, true, "Raquel" },
                    { 22, true, "Juan Lemus" },
                    { 23, true, "Miguel García" },
                    { 24, true, "Juan Calderón" },
                    { 25, true, "Artz" },
                    { 26, true, "Paw Printex" }
                });

            migrationBuilder.InsertData(
                table: "ProveedorProcesos",
                columns: new[] { "ProcesoId", "ProveedorId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 4, 2 },
                    { 3, 3 },
                    { 2, 4 },
                    { 2, 5 },
                    { 2, 6 },
                    { 2, 7 },
                    { 2, 8 },
                    { 2, 9 },
                    { 2, 10 },
                    { 2, 11 },
                    { 2, 12 },
                    { 2, 13 },
                    { 2, 14 },
                    { 2, 15 },
                    { 2, 16 },
                    { 2, 17 },
                    { 2, 18 },
                    { 2, 19 },
                    { 2, 20 },
                    { 2, 21 },
                    { 1, 22 },
                    { 1, 23 },
                    { 1, 24 },
                    { 3, 25 },
                    { 3, 26 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProveedorProcesos_ProcesoId",
                table: "ProveedorProcesos",
                column: "ProcesoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProveedorProcesos");

            migrationBuilder.DeleteData(
                table: "Procesos",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Procesos",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Procesos",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Procesos",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Nombre",
                value: "Aero");

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 10,
                column: "Nombre",
                value: "Lob");
        }
    }
}
