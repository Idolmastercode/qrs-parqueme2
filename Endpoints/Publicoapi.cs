using Microsoft.EntityFrameworkCore;
using qrs_back.Data;
using qrs_back.Entities;

namespace qrs_back.Endpoints;

public record ConteoTalla(string? Talla, int Primeras, int Segundas);
public record ConteoProveedor(List<ConteoTalla>? Tallas, string? Comentario, string? CitaPropuesta);

/// <summary>
/// Página del proveedor servida DESDE LA OFICINA (para probar en la red local).
/// La de verdad vive en el puente de IONOS y se comporta igual:
///
/// GET  /api/t/{token}   datos de su orden
/// POST /api/t/{token}   manda (o corrige) su conteo y propone día y hora
///
/// Estados que ve el proveedor:
///   abierta     sin cita confirmada → puede mandar y corregir
///   programada  cita confirmada → solo ve su cita
///   entregada   ya se registró lo que llegó → "pedido entregado"
/// Sin precios, sin cliente, sin otras órdenes.
/// </summary>
public static class PublicoApi
{
    public static void MapPublico(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/t").RequireRateLimiting("publico");

        g.MapGet("/{token}", async (string token, AppDb db) =>
        {
            var o = await Buscar(db, token);
            return o is null ? NoValido() : Results.Ok(await Vista(db, o));
        });

        g.MapPost("/{token}", async (string token, ConteoProveedor c, AppDb db) =>
        {
            var o = await Buscar(db, token);
            if (o is null) return NoValido();
            if (o.EntregadaEn is not null)
                return Resp.Choca("Este pedido ya se entregó. Si hay algún cambio, comunícate con nosotros.");
            if (o.CitaConfirmada is not null)
                return Resp.Choca("Tu entrega ya está confirmada y el conteo ya no se puede cambiar. Si hay algún cambio, comunícate con nosotros.");

            // Conteo: exactamente las tallas de la orden
            var conteo = c.Tallas ?? [];
            foreach (var x in conteo)
                if (!o.Tallas.Any(t => Igual(t.Talla, x.Talla))) return Resp.Mal($"La talla {x.Talla} no está en esta orden.");
            foreach (var t in o.Tallas)
            {
                var x = conteo.FirstOrDefault(y => Igual(y.Talla, t.Talla));
                if (x is null) return Resp.Mal($"Falta el conteo de la talla {t.Talla}.");
                if (x.Primeras < 0 || x.Segundas < 0) return Resp.Mal($"Talla {t.Talla}: no puede haber cantidades negativas.");
                if (x.Primeras + x.Segundas > t.Pedidas)
                    return Resp.Mal($"Talla {t.Talla}: son {x.Primeras + x.Segundas} piezas y solo se pidieron {t.Pedidas}.");
            }
            if (OrdenesApi.RevisarComentario(c.Comentario) is string e1) return Resp.Mal(e1);

            // Día y hora (hora de México)
            if (string.IsNullOrWhiteSpace(c.CitaPropuesta)) return Resp.Mal("Elige el día y la hora de entrega.");
            if (OrdenesApi.LeerCita(c.CitaPropuesta) is not DateTime cita) return Resp.Mal("La fecha de entrega no es válida.");
            var ahora = Hora.Mexico();
            if (cita < ahora) return Resp.Mal("Esa hora ya pasó, elige otra.");
            if (cita > ahora.AddDays(90)) return Resp.Mal("La fecha de entrega está demasiado lejos.");

            foreach (var t in o.Tallas)
            {
                var x = conteo.First(y => Igual(y.Talla, t.Talla));
                t.Primeras = x.Primeras;
                t.Segundas = x.Segundas;
            }
            o.Comentario = OrdenesApi.LimpiarComentario(c.Comentario);
            o.CitaPropuesta = cita;
            o.RecibidoEn = DateTime.UtcNow;

            // Si los precios especiales ya no cuadran con el conteo nuevo, se quitan
            var tallas = o.Tallas.Select(t => new TallaDto(t.Talla, t.Pedidas, t.Primeras, t.Segundas)).ToList();
            var lotes = o.LotesSegundas.Select(l => new LoteDto(l.Cantidad, l.PrecioCentavos, l.Talla, l.Motivo)).ToList();
            if (OrdenesApi.RevisarLotes(lotes, tallas) is not null) o.LotesSegundas.Clear();

            await db.SaveChangesAsync();
            return Results.Ok(await Vista(db, o));
        });
    }

    private static async Task<Orden?> Buscar(AppDb db, string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 32) return null;
        return await db.Ordenes
            .Include(x => x.Tallas)
            .Include(x => x.LotesSegundas)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Token == token);
    }

    private static async Task<object> Vista(AppDb db, Orden o)
    {
        var d = await db.Ordenes
            .Where(x => x.Id == o.Id)
            .Select(x => new
            {
                Empresa = x.Produccion.Empresa.Nombre, x.Produccion.Op, x.Produccion.Modelo,
                Proceso = x.Proceso.Nombre, Proveedor = x.Proveedor.Nombre,
            })
            .FirstAsync();

        return new
        {
            estado = o.EntregadaEn is not null ? "entregada" : o.CitaConfirmada is not null ? "programada" : "abierta",
            empresa = d.Empresa,
            op = d.Op,
            operacion = o.Np,
            modelo = d.Modelo,
            proceso = d.Proceso,
            proveedor = d.Proveedor,
            tallas = o.Tallas.OrderBy(t => t.Posicion).Select(t => new { talla = t.Talla, pedidas = t.Pedidas, primeras = t.Primeras, segundas = t.Segundas }),
            comentario = o.Comentario ?? "",
            recibidoEn = o.RecibidoEn,
            citaPropuesta = OrdenesApi.Cita(o.CitaPropuesta),
            citaConfirmada = OrdenesApi.Cita(o.CitaConfirmada),
            entregadaEn = o.EntregadaEn,
            pedidoCambio = o.PedidoCorregidoEn is not null && o.RecibidoEn is null,
        };
    }

    private static bool Igual(string? a, string? b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static IResult NoValido() => Results.Json(
        new { error = "Este enlace ya no es válido. Pide uno nuevo a quien te mandó el pedido." },
        statusCode: StatusCodes.Status404NotFound);
}