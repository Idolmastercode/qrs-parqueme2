using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
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

// Página pública del proveedor: máximo 30 llamadas por minuto por IP,
// para que nadie pueda ponerse a probar enlaces al azar.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("publico", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // documento en /openapi/v1.json
}

app.UseCors("panel");
app.UseRateLimiter();

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
app.MapPublico();

// Página del proveedor: el QR apunta aquí (en producción: https://pedidos.cartoonia.mx/t/{token}).
// Es un solo archivo en wwwroot; la página lee el token de la dirección y llama a /api/t/{token}.
var paginaProveedor = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "proveedor.html");
app.MapGet("/t/{token}", () => Results.File(paginaProveedor, "text/html; charset=utf-8"));

app.Run();
