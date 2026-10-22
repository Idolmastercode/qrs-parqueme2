using qrs_back.Data;
using qrs_back.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(o => o
    .UseSqlite(builder.Configuration.GetConnectionString("Qrs"))
    // Tallas y lotes dependen de Orden, que tiene filtro de borrado lógico. Es intencional: si la orden
    // está oculta, sus tallas también. EF lo advierte por si fuera un error; aquí no lo es.
    .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

builder.Services.AddOpenApi();

// Permite que el panel en React (Vite) llame a la API mientras desarrollas
builder.Services.AddCors(o => o.AddPolicy("panel", p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // documento en /openapi/v1.json
}

app.UseCors("panel");

// Prueba de vida: si esto responde con las 4 empresas, la base y la migración están bien.
app.MapGet("/api/empresas", async (AppDb db) =>
    await db.Empresas
        .Where(e => e.Activo)
        .OrderBy(e => e.Nombre)
        .Select(e => new { e.Id, e.Nombre })
        .ToListAsync());

app.MapCatalogos();
app.MapProducciones();
app.MapOrdenes();

app.Run();