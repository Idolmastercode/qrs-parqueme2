using Microsoft.EntityFrameworkCore;
using qrs_back.Data;
using qrs_back.Entities;

namespace qrs_back.Endpoints;

public record ConteoTalla(string? Talla, int Primeras, int Segundas);

/// <summary>Lo que manda el proveedor desde su página: conteo por talla, comentario y cuándo entrega.</summary>
public record ConteoProveedor(List<ConteoTalla>? Tallas, string? Comentario, string? CitaPropuesta);

/// <summary>
/// Lo que usa la página del proveedor (la que abre el QR). Sin login: el token ES el permiso.
///
/// GET  /api/t/{token}   datos de su orden
/// POST /api/t/{token}   manda (o corrige) su conteo y propone día y hora de entrega
///
/// Estados que ve el proveedor:
///   abierta     sin cita confirmada → puede mandar y corregir cuantas veces quiera
///   programada  cita confirmada en el futuro → solo ve su cita
///   cerrada     la cita ya pasó → "pedido cerrado"
/// Solo ve lo que le toca: sin precios, sin cliente, sin otras órdenes.
/// </summary>
public static class PublicoApi
{
    public static void MapPublico(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/t").RequireRateLimiting("publico");

        g.MapGet("/{token}", async (string token, AppDb db) =>
            await Buscar(db, token) is Orden o ? Results.Ok(Vista(o)) : NoValido());

        g.MapPost("/{token}", async (string token, ConteoProveedor c, AppDb db) =>
        {
            var o = await Buscar(db, token);
            if (o is null) return NoValido();
            if (o.CitaConfirmada is not null)
                return Resp.Choca("Tu entrega ya está confirmada y el conteo ya no se puede cambiar. Si hay algún cambio, comunícate con nosotros.");

            // ---- Conteo: exactamente las tallas de la orden ----
            var conteo = c.Tallas ?? [];
            foreach (var x in conteo)
                if (Talla(o, x.Talla) is null) return Resp.Mal($"La talla {x.Talla} no está en esta orden.");

            foreach (var t in o.Tallas)
            {
                var x = conteo.FirstOrDefault(y => Igual(y.Talla, t.Talla));
                if (x is null) return Resp.Mal($"Falta el conteo de la talla {t.Talla}.");
                if (x.Primeras < 0 || x.Segundas < 0) return Resp.Mal($"Talla {t.Talla}: no puede haber cantidades negativas.");
                if (x.Primeras + x.Segundas > t.Pedidas)
                    return Resp.Mal($"Talla {t.Talla}: son {x.Primeras + x.Segundas} piezas y solo se pidieron {t.Pedidas}.");
            }
            if (OrdenesApi.RevisarComentario(c.Comentario) is string e1) return Resp.Mal(e1);

            // ---- Día y hora de entrega (hora de México) ----
            if (string.IsNullOrWhiteSpace(c.CitaPropuesta)) return Resp.Mal("Elige el día y la hora de entrega.");
            if (OrdenesApi.LeerCita(c.CitaPropuesta) is not DateTime cita) return Resp.Mal("La fecha de entrega no es válida.");
            var ahora = Hora.Mexico();
            if (cita < ahora) return Resp.Mal("Esa hora ya pasó, elige otra.");
            if (cita > ahora.AddDays(90)) return Resp.Mal("La fecha de entrega está demasiado lejos.");

            // ---- Guardar ----
            foreach (var t in o.Tallas)
            {
                var x = conteo.First(y => Igual(y.Talla, t.Talla));
                t.Primeras = x.Primeras;
                t.Segundas = x.Segundas;
            }
            o.Comentario = OrdenesApi.LimpiarComentario(c.Comentario);
            o.CitaPropuesta = cita;
            o.RecibidoEn = DateTime.UtcNow;

            // Si corrigió y ya no cuadran los precios especiales que ustedes habían puesto
            // (p. ej. bajaron las segundas), se quitan para que los vuelvan a revisar.
            var tallas = o.Tallas.Select(t => new TallaDto(t.Talla, t.Pedidas, t.Primeras, t.Segundas)).ToList();
            var lotes = o.LotesSegundas.Select(l => new LoteDto(l.Cantidad, l.PrecioCentavos, l.Talla, l.Motivo)).ToList();
            if (OrdenesApi.RevisarLotes(lotes, tallas) is not null) o.LotesSegundas.Clear();

            await db.SaveChangesAsync();
            return Results.Ok(Vista(o));
        });
    }

    private static async Task<Orden?> Buscar(AppDb db, string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 32) return null;
        return await db.Ordenes
            .Include(o => o.Produccion).ThenInclude(p => p.Empresa)
            .Include(o => o.Proceso)
            .Include(o => o.Proveedor)
            .Include(o => o.Tallas)
            .Include(o => o.LotesSegundas)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Token == token);
    }

    private static string Estado(Orden o) =>
        o.CitaConfirmada is null ? "abierta"
        : o.CitaConfirmada > Hora.Mexico() ? "programada"
        : "cerrada";

    private static object Vista(Orden o) => new
    {
        estado = Estado(o),
        empresa = o.Produccion.Empresa.Nombre,
        op = o.Produccion.Op,
        operacion = o.Np,
        modelo = o.Produccion.Modelo,
        proceso = o.Proceso.Nombre,
        proveedor = o.Proveedor.Nombre,
        tallas = o.Tallas.OrderBy(t => t.Posicion).Select(t => new { t.Talla, t.Pedidas, t.Primeras, t.Segundas }).ToList(),
        comentario = o.Comentario ?? "",
        recibidoEn = o.RecibidoEn,
        citaPropuesta = OrdenesApi.Cita(o.CitaPropuesta),
        citaConfirmada = OrdenesApi.Cita(o.CitaConfirmada),
    };

    private static IResult NoValido() =>
        Results.NotFound(new { error = "Este enlace ya no es válido. Pide uno nuevo a quien te mandó el pedido." });

    private static bool Igual(string? a, string? b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static OrdenTalla? Talla(Orden o, string? nombre) => o.Tallas.FirstOrDefault(t => Igual(t.Talla, nombre));
}