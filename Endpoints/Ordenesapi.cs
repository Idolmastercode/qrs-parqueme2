using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using qrs_back.Data;
using qrs_back.Entities;

namespace qrs_back.Endpoints;

public record TallaDto(string Talla, int Pedidas, int Primeras, int Segundas);

public record LoteDto(int Cantidad, long PrecioCentavos, string? Talla, string? Motivo);

/// <summary>Misma forma que MOCK_ORDENES del front. Citas como "AAAA-MM-DDTHH:mm" (hora local).</summary>
public record OrdenOut(
    int Id, int ProduccionId, long Np, int ProcesoId, int ProveedorId,
    long PrecioCentavos, long PrecioSegundaCentavos, string Comentario,
    string? Token, DateTime? RecibidoEn, string? CitaPropuesta, string? CitaConfirmada,
    DateTime CreadoEn, List<TallaDto> Tallas, List<LoteDto> LotesSegundas);

/// <summary>Nueva orden. Si la OP ya existe manda produccionId; si es nueva, manda nuevaProduccion.</summary>
public record OrdenNueva(
    int? ProduccionId, ProduccionIn? NuevaProduccion,
    long Np, int ProcesoId, int ProveedorId, long PrecioCentavos, long PrecioSegundaCentavos,
    List<TallaDto>? Tallas, string? Comentario);

/// <summary>Cambios parciales: solo se toca lo que venga. Sirve para "Editar operación" y para guardar el conteo.</summary>
public record OrdenCambios(
    long? Np, int? ProcesoId, int? ProveedorId, long? PrecioCentavos, long? PrecioSegundaCentavos,
    List<TallaDto>? Tallas, string? Comentario, List<LoteDto>? LotesSegundas);

/// <summary>null o "" quita la cita (y el formulario del proveedor vuelve a abrirse).</summary>
public record CitaIn(string? CitaConfirmada);

/// <summary>
/// GET    /api/ordenes                lista con tallas y lotes
/// GET    /api/ordenes/{id}
/// POST   /api/ordenes                crear (y crear la OP si es nueva)  → { orden, produccion }
/// PATCH  /api/ordenes/{id}           editar datos y/o conteo
/// DELETE /api/ordenes/{id}           borrado lógico
/// POST   /api/ordenes/{id}/enlace    generar enlace QR (si ya tiene, regresa el mismo)
/// DELETE /api/ordenes/{id}/enlace    desactivar enlace
/// PUT    /api/ordenes/{id}/cita      { citaConfirmada }
/// </summary>
public static class OrdenesApi
{
    public static void MapOrdenes(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/ordenes");

        g.MapGet("/", async (AppDb db) =>
        {
            var lista = await ConDetalle(db).AsNoTracking().OrderBy(o => o.Id).ToListAsync();
            return lista.Select(Out).ToList();
        });

        g.MapGet("/{id:int}", async (int id, AppDb db) =>
            await ConDetalle(db).AsNoTracking().FirstOrDefaultAsync(o => o.Id == id) is Orden o
                ? Results.Ok(Out(o))
                : Results.NotFound());

        // ---------- Crear ----------
        g.MapPost("/", async (OrdenNueva c, AppDb db) =>
        {
            Produccion? prod;
            var opNueva = false;

            if (c.ProduccionId is int pid)
            {
                prod = await db.Producciones.FirstOrDefaultAsync(p => p.Id == pid);
                if (prod is null) return Resp.Mal("Esa OP no existe.");
            }
            else if (c.NuevaProduccion is ProduccionIn nueva)
            {
                if (await ProduccionesApi.Revisar(db, nueva, null) is IResult e0) return e0;
                prod = new Produccion
                {
                    EmpresaId = nueva.EmpresaId,
                    Op = nueva.Op,
                    Modelo = ProduccionesApi.LimpiarModelo(nueva.Modelo),
                    ClienteId = nueva.ClienteId,
                };
                db.Producciones.Add(prod);
                opNueva = true;
            }
            else return Resp.Mal("Falta la OP.");

            var excepto = (int?)null;
            if (await RevisarCampos(db, prod.EmpresaId, c.Np, c.ProcesoId, c.ProveedorId, c.PrecioCentavos, c.PrecioSegundaCentavos, excepto) is IResult e1) return e1;

            var tallas = LimpiarTallas(c.Tallas);
            if (RevisarTallas(tallas) is string e2) return Resp.Mal(e2);
            if (RevisarComentario(c.Comentario) is string e3) return Resp.Mal(e3);

            var orden = new Orden
            {
                Produccion = prod,
                Np = c.Np,
                ProcesoId = c.ProcesoId,
                ProveedorId = c.ProveedorId,
                PrecioCentavos = c.PrecioCentavos,
                PrecioSegundaCentavos = c.PrecioSegundaCentavos,
                Comentario = LimpiarComentario(c.Comentario),
            };
            AplicarTallas(orden, tallas);
            db.Ordenes.Add(orden);
            await db.SaveChangesAsync();

            return Results.Created($"/api/ordenes/{orden.Id}", new
            {
                orden = Out(orden),
                produccion = opNueva ? ProduccionesApi.Out(prod) : null,
            });
        });

        // ---------- Editar (datos y/o conteo) ----------
        g.MapPatch("/{id:int}", async (int id, OrdenCambios c, AppDb db) =>
        {
            var o = await ConDetalle(db).FirstOrDefaultAsync(x => x.Id == id);
            if (o is null) return Results.NotFound();
            var empresaId = await db.Producciones.Where(p => p.Id == o.ProduccionId).Select(p => p.EmpresaId).FirstAsync();

            var np = c.Np ?? o.Np;
            var procesoId = c.ProcesoId ?? o.ProcesoId;
            var proveedorId = c.ProveedorId ?? o.ProveedorId;
            var precio = c.PrecioCentavos ?? o.PrecioCentavos;
            var precioSegunda = c.PrecioSegundaCentavos ?? o.PrecioSegundaCentavos;
            if (await RevisarCampos(db, empresaId, np, procesoId, proveedorId, precio, precioSegunda, id) is IResult e1) return e1;

            // Lo que no venga se valida con lo que ya tiene: si bajan las segundas, los lotes viejos también deben cuadrar
            var tallas = c.Tallas is null
                ? o.Tallas.OrderBy(t => t.Posicion).Select(t => new TallaDto(t.Talla, t.Pedidas, t.Primeras, t.Segundas)).ToList()
                : LimpiarTallas(c.Tallas);
            if (RevisarTallas(tallas) is string e2) return Resp.Mal(e2);

            var lotes = c.LotesSegundas is null
                ? o.LotesSegundas.Select(l => new LoteDto(l.Cantidad, l.PrecioCentavos, l.Talla, l.Motivo)).ToList()
                : LimpiarLotes(c.LotesSegundas);
            if (RevisarLotes(lotes, tallas) is string e3) return Resp.Mal(e3);
            if (RevisarComentario(c.Comentario) is string e4) return Resp.Mal(e4);

            o.Np = np;
            o.ProcesoId = procesoId;
            o.ProveedorId = proveedorId;
            o.PrecioCentavos = precio;
            o.PrecioSegundaCentavos = precioSegunda;
            if (c.Comentario is not null) o.Comentario = LimpiarComentario(c.Comentario);
            if (c.Tallas is not null) AplicarTallas(o, tallas);
            if (c.LotesSegundas is not null)
            {
                o.LotesSegundas.Clear();
                o.LotesSegundas.AddRange(lotes.Select(l => new LoteSegundas
                {
                    Cantidad = l.Cantidad,
                    PrecioCentavos = l.PrecioCentavos,
                    Talla = l.Talla,
                    Motivo = l.Motivo,
                }));
            }

            await db.SaveChangesAsync();
            return Results.Ok(Out(o));
        });

        // ---------- Borrar ----------
        g.MapDelete("/{id:int}", async (int id, AppDb db) =>
        {
            var o = await db.Ordenes.FirstOrDefaultAsync(x => x.Id == id);
            if (o is null) return Results.NotFound();
            o.EliminadoEn = DateTime.UtcNow;
            o.Token = null; // el enlace deja de funcionar
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // ---------- Enlace QR ----------
        g.MapPost("/{id:int}/enlace", async (int id, AppDb db) =>
        {
            var o = await ConDetalle(db).FirstOrDefaultAsync(x => x.Id == id);
            if (o is null) return Results.NotFound();
            if (o.Token is null)
            {
                o.Token = await TokenLibre(db);
                o.TokenCreadoEn = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            return Results.Ok(Out(o));
        });

        g.MapDelete("/{id:int}/enlace", async (int id, AppDb db) =>
        {
            var o = await ConDetalle(db).FirstOrDefaultAsync(x => x.Id == id);
            if (o is null) return Results.NotFound();
            o.Token = null;
            o.TokenCreadoEn = null;
            await db.SaveChangesAsync();
            return Results.Ok(Out(o));
        });

        // ---------- Cita ----------
        g.MapPut("/{id:int}/cita", async (int id, CitaIn c, AppDb db) =>
        {
            var o = await ConDetalle(db).FirstOrDefaultAsync(x => x.Id == id);
            if (o is null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(c.CitaConfirmada)) o.CitaConfirmada = null;
            else if (LeerCita(c.CitaConfirmada) is DateTime f) o.CitaConfirmada = f;
            else return Resp.Mal("La cita debe venir como AAAA-MM-DDTHH:mm.");

            await db.SaveChangesAsync();
            return Results.Ok(Out(o));
        });
    }

    // =====================================================================
    //  Consultas y salida
    // =====================================================================

    private static IQueryable<Orden> ConDetalle(AppDb db) =>
        db.Ordenes.Include(o => o.Tallas).Include(o => o.LotesSegundas).AsSplitQuery();

    private static OrdenOut Out(Orden o) => new(
        o.Id, o.ProduccionId, o.Np, o.ProcesoId, o.ProveedorId,
        o.PrecioCentavos, o.PrecioSegundaCentavos, o.Comentario ?? "",
        o.Token, o.RecibidoEn, Cita(o.CitaPropuesta), Cita(o.CitaConfirmada),
        o.CreadoEn,
        o.Tallas.OrderBy(t => t.Posicion).Select(t => new TallaDto(t.Talla, t.Pedidas, t.Primeras, t.Segundas)).ToList(),
        o.LotesSegundas.OrderBy(l => l.Id).Select(l => new LoteDto(l.Cantidad, l.PrecioCentavos, l.Talla, l.Motivo)).ToList());

    private static string? Cita(DateTime? d) => d?.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture);

    private static readonly string[] FormatosCita = ["yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss"];

    private static DateTime? LeerCita(string s) =>
        DateTime.TryParseExact(s.Trim(), FormatosCita, CultureInfo.InvariantCulture, DateTimeStyles.None, out var f)
            ? f
            : null;

    // =====================================================================
    //  Validaciones (null = todo bien)
    // =====================================================================

    private static async Task<IResult?> RevisarCampos(
        AppDb db, int empresaId, long np, int procesoId, int proveedorId, long precio, long precioSegunda, int? excepto)
    {
        if (np <= 0) return Resp.Mal("La operación debe ser un número mayor a 0.");
        if (precio < 0 || precioSegunda < 0) return Resp.Mal("Los precios no pueden ser negativos.");
        if (!await db.Procesos.AnyAsync(x => x.Id == procesoId)) return Resp.Mal("Ese proceso no existe.");
        if (!await db.Proveedores.AnyAsync(x => x.Id == proveedorId)) return Resp.Mal("Ese proveedor no existe.");

        // Operación única por empresa (no lo puede forzar SQLite porque la empresa vive en la producción)
        if (await db.Ordenes.AnyAsync(o => o.Np == np && o.Produccion.EmpresaId == empresaId && o.Id != excepto))
            return Resp.Choca($"La operación {np} ya existe en esa empresa.");
        return null;
    }

    private static string? RevisarTallas(List<TallaDto> tallas)
    {
        if (tallas.Count == 0) return "Agrega al menos una talla.";
        var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tallas)
        {
            if (t.Talla.Length == 0 || t.Talla.Length > 10) return "Cada talla necesita un nombre de hasta 10 caracteres.";
            if (!vistas.Add(t.Talla)) return $"La talla {t.Talla} está repetida.";
            if (t.Pedidas < 0 || t.Primeras < 0 || t.Segundas < 0) return $"Talla {t.Talla}: no puede haber cantidades negativas.";
            if (t.Primeras + t.Segundas > t.Pedidas)
                return $"Talla {t.Talla}: primeras + segundas ({t.Primeras + t.Segundas}) pasan de las pedidas ({t.Pedidas}).";
        }
        return null;
    }

    /// <summary>Igual que erroresLotes del front: por talla, no puede haber más con precio especial que segundas.</summary>
    private static string? RevisarLotes(List<LoteDto> lotes, List<TallaDto> tallas)
    {
        var segundasPorTalla = tallas.ToDictionary(t => t.Talla, t => t.Segundas, StringComparer.Ordinal);
        foreach (var l in lotes)
        {
            if (l.Cantidad <= 0) return "Cada precio especial necesita al menos 1 pieza.";
            if (l.PrecioCentavos < 0) return "Los precios no pueden ser negativos.";
            if (l.Motivo is { Length: > 200 }) return "El motivo puede tener hasta 200 caracteres.";
            if (l.Talla is not null && !segundasPorTalla.ContainsKey(l.Talla)) return $"La talla {l.Talla} no está en esta orden.";
        }

        var totalSegundas = tallas.Sum(t => t.Segundas);
        var totalLotes = lotes.Sum(l => l.Cantidad);
        if (totalLotes > totalSegundas)
            return $"Hay {totalLotes} segundas con precio especial pero solo {totalSegundas} de segunda.";

        foreach (var grupo in lotes.Where(l => l.Talla is not null).GroupBy(l => l.Talla!))
        {
            var n = grupo.Sum(l => l.Cantidad);
            var hay = segundasPorTalla[grupo.Key];
            if (n > hay) return $"{grupo.Key}: {n} con precio especial pero solo hay {hay} de segunda.";
        }
        return null;
    }

    private static string? RevisarComentario(string? s) =>
        s is { Length: > 1000 } ? "El comentario puede tener hasta 1000 caracteres." : null;

    // =====================================================================
    //  Limpieza y aplicación
    // =====================================================================

    private static List<TallaDto> LimpiarTallas(List<TallaDto>? tallas) =>
        (tallas ?? []).Select(t => t with { Talla = (t.Talla ?? "").Trim().ToUpperInvariant() }).ToList();

    private static List<LoteDto> LimpiarLotes(List<LoteDto> lotes) =>
        lotes.Select(l => l with
        {
            Talla = string.IsNullOrWhiteSpace(l.Talla) ? null : l.Talla.Trim().ToUpperInvariant(),
            Motivo = string.IsNullOrWhiteSpace(l.Motivo) ? null : l.Motivo.Trim(),
        }).ToList();

    private static string? LimpiarComentario(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>
    /// Actualiza las tallas en su lugar (por nombre) en vez de borrar y recrear,
    /// para no chocar con el índice único (OrdenId, Talla).
    /// </summary>
    private static void AplicarTallas(Orden o, List<TallaDto> tallas)
    {
        var nuevas = new HashSet<string>(tallas.Select(t => t.Talla), StringComparer.OrdinalIgnoreCase);
        o.Tallas.RemoveAll(t => !nuevas.Contains(t.Talla));
        var actuales = o.Tallas.ToDictionary(t => t.Talla, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < tallas.Count; i++)
        {
            var t = tallas[i];
            if (!actuales.TryGetValue(t.Talla, out var x))
            {
                x = new OrdenTalla();
                o.Tallas.Add(x);
            }
            x.Talla = t.Talla;
            x.Posicion = i;
            x.Pedidas = t.Pedidas;
            x.Primeras = t.Primeras;
            x.Segundas = t.Segundas;
        }
    }

    // =====================================================================
    //  Token del enlace: 10 caracteres al azar sin 0/O/1/l/i para que se pueda dictar ("k7f2m-9qxae")
    // =====================================================================

    private const string Alfabeto = "23456789abcdefghjkmnpqrstuvwxyz";

    private static async Task<string> TokenLibre(AppDb db)
    {
        while (true)
        {
            var s = RandomNumberGenerator.GetString(Alfabeto, 10);
            var token = $"{s[..5]}-{s[5..]}";
            // Incluye órdenes borradas: un enlace viejo nunca debe abrir una orden distinta
            if (!await db.Ordenes.IgnoreQueryFilters().AnyAsync(o => o.Token == token)) return token;
        }
    }
}