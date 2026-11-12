using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using qrs_back.Data;
using qrs_back.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(o => o
    .UseSqlite(builder.Configuration.GetConnectionString("Qrs"))

    .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

builder.Services.AddOpenApi();

builder.Services.AddCors(o => o.AddPolicy("panel", p => p
    .SetIsOriginAllowed(origen => builder.Environment.IsDevelopment()
        ? EsRedLocal(origen)
        : origen == "https://sistema.cartoonia.mx")
    .AllowAnyHeader()
    .AllowAnyMethod()));

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
    app.MapOpenApi();
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

static bool EsRedLocal(string origen)
{
    if (!Uri.TryCreate(origen, UriKind.Absolute, out var u)) return false;
    var h = u.Host;
    return h == "localhost" || h == "127.0.0.1" || h.StartsWith("192.168.") || h.StartsWith("10.")
        || (h.StartsWith("172.") && int.TryParse(h.Split('.')[1], out var b) && b >= 16 && b <= 31);
}