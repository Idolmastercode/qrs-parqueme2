using Microsoft.EntityFrameworkCore;
using qrs_back.Data;
using qrs_back.Entities;

namespace qrs_back.Endpoints;

public record ProduccionOut(int Id, int EmpresaId, long Op, string Modelo, int ClienteId, DateTime CreadoEn, DateTime? CompletadaEn);

public record ProduccionIn(int EmpresaId, long Op, string? Modelo, int ClienteId);

/// <summary>
/// GET  /api/producciones                lista (misma forma que MOCK_PRODUCCIONES)
/// PUT  /api/producciones/{id}           editar OP: { empresaId, op, modelo, clienteId }
/// POST /api/producciones/{id}/completar
/// POST /api/producciones/{id}/reabrir
/// Las producciones se crean junto con su primera orden (POST /api/ordenes con nuevaProduccion).
/// </summary>
public static class ProduccionesApi
{
    public static void MapProducciones(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/producciones");

        g.MapGet("/", async (AppDb db) =>
            await db.Producciones
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => new ProduccionOut(p.Id, p.EmpresaId, p.Op, p.Modelo, p.ClienteId, p.CreadoEn, p.CompletadaEn))
                .ToListAsync());

        g.MapPut("/{id:int}", async (int id, ProduccionIn cuerpo, AppDb db) =>
        {
            var p = await db.Producciones.FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return Results.NotFound();

            if (await Revisar(db, cuerpo, id) is IResult error) return error;

            // Si cambia de empresa, sus operaciones no pueden chocar con las de la empresa nueva
            if (cuerpo.EmpresaId != p.EmpresaId)
            {
                var nps = await db.Ordenes.Where(o => o.ProduccionId == id).Select(o => o.Np).ToListAsync();
                var repetida = await db.Ordenes
                    .Where(o => o.Produccion.EmpresaId == cuerpo.EmpresaId && nps.Contains(o.Np))
                    .Select(o => (long?)o.Np)
                    .FirstOrDefaultAsync();
                if (repetida is long np)
                    return Resp.Choca($"La operación {np} ya existe en la otra empresa.");
            }

            p.EmpresaId = cuerpo.EmpresaId;
            p.Op = cuerpo.Op;
            p.Modelo = LimpiarModelo(cuerpo.Modelo);
            p.ClienteId = cuerpo.ClienteId;
            await db.SaveChangesAsync();
            return Results.Ok(Out(p));
        });

        g.MapPost("/{id:int}/completar", (int id, AppDb db) => Marcar(db, id, completada: true));
        g.MapPost("/{id:int}/reabrir", (int id, AppDb db) => Marcar(db, id, completada: false));
    }

    private static async Task<IResult> Marcar(AppDb db, int id, bool completada)
    {
        var p = await db.Producciones.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return Results.NotFound();
        // Completar dos veces no le cambia la fecha
        p.CompletadaEn = completada ? p.CompletadaEn ?? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
        return Results.Ok(Out(p));
    }

    /// <summary>Valida una OP nueva o editada. Null = todo bien. La usa también OrdenesApi al crear OP.</summary>
    internal static async Task<IResult?> Revisar(AppDb db, ProduccionIn c, int? excepto)
    {
        if (c.Op <= 0) return Resp.Mal("La OP debe ser un número mayor a 0.");
        var modelo = LimpiarModelo(c.Modelo);
        if (modelo.Length == 0 || modelo.Length > 40) return Resp.Mal("Escribe el modelo (hasta 40 caracteres).");
        if (!await db.Empresas.AnyAsync(e => e.Id == c.EmpresaId)) return Resp.Mal("Esa empresa no existe.");
        if (!await db.Clientes.AnyAsync(x => x.Id == c.ClienteId)) return Resp.Mal("Ese cliente no existe.");
        if (await db.Producciones.AnyAsync(p => p.EmpresaId == c.EmpresaId && p.Op == c.Op && p.Id != excepto))
            return Resp.Choca($"La OP {c.Op} ya existe en esa empresa.");
        return null;
    }

    /// <summary>Igual que el front: sin espacios y en mayúsculas ("sonic 213" → "SONIC213").</summary>
    internal static string LimpiarModelo(string? s) =>
        string.Concat((s ?? "").Where(ch => !char.IsWhiteSpace(ch))).ToUpperInvariant();

    internal static ProduccionOut Out(Produccion p) =>
        new(p.Id, p.EmpresaId, p.Op, p.Modelo, p.ClienteId, p.CreadoEn, p.CompletadaEn);
}