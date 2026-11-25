using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using qrs_back.Data;
using qrs_back.Entities;

namespace qrs_back.Endpoints;

/// <summary>
/// SOLO PARA PRUEBAS (solo existe cuando corres en Development, o sea con "dotnet run").
///
/// GET /api/dev/sembrar?ops=40
///   Crea OPs al azar con 1 a 4 operaciones cada una, en todos los estados:
///   sin enlace, esperando proveedor, por agendar, programadas (citas en los próximos 3 semanas)
///   y entregadas (citas de las últimas 3 semanas). Sirve para ver el calendario con volumen.
///   Se puede llamar varias veces (suma más). Para borrar todo: limpiar-ordenes.sql.
/// </summary>
public static class DevApi
{
    private static readonly string[] Modelos =
    [
        "NAVIDADGRINCH", "SONICKIDS", "POLOBASICO", "JOGGERFLEECE", "PIJAMAKIDS", "GORRAESC", "HOODIEMARVEL", "SUDADERAUNAM",
        "PLAYERAMICKEY", "SHORTPLAYA", "BLUSALINO", "CHAMARRAJEAN", "BODYBEBE", "MALLASPORT", "VESTIDOFLOR", "PANTALONCARGO",
    ];

    private static readonly string[][] Juegos =
    [
        ["CH", "M", "G", "EG"],
        ["CH", "M", "G"],
        ["S", "M", "L", "XL"],
        ["2", "4", "6", "8", "10", "12"],
        ["30", "32", "34", "36", "38"],
        ["6M", "12M", "18M"],
    ];

    public static void MapDev(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;

        app.MapGet("/api/dev/sembrar", async (int? ops, AppDb db) =>
        {
            var n = Math.Clamp(ops ?? 40, 1, 300);
            var r = new Random();
            var empresas = await db.Empresas.Select(x => x.Id).ToListAsync();
            var clientes = await db.Clientes.Where(x => x.Activo).Select(x => x.Id).ToListAsync();
            var procesos = await db.Procesos.Where(x => x.Activo).Select(x => x.Id).ToListAsync();
            var proveedores = await db.Proveedores.Where(x => x.Activo).Select(x => x.Id).ToListAsync();
            var hacen = await db.ProveedorProcesos.Select(x => new { x.ProveedorId, x.ProcesoId }).ToListAsync();
            if (empresas.Count == 0 || clientes.Count == 0 || procesos.Count == 0 || proveedores.Count == 0)
                return Resp.Mal("Faltan catálogos (empresas, clientes, procesos o proveedores).");

            // Números que no choquen con lo que ya hay
            var op = (await db.Producciones.IgnoreQueryFilters().MaxAsync(x => (long?)x.Op) ?? 999) + 1;
            var np = (await db.Ordenes.IgnoreQueryFilters().MaxAsync(x => (long?)x.Np) ?? 4999) + 1;
            var hoy = Hora.Mexico().Date;
            var cuantas = 0;

            for (var i = 0; i < n; i++)
            {
                var prod = new Produccion
                {
                    EmpresaId = empresas[r.Next(empresas.Count)],
                    Op = op++,
                    Modelo = Modelos[r.Next(Modelos.Length)] + r.Next(10, 99),
                    ClienteId = clientes[r.Next(clientes.Count)],
                };
                db.Producciones.Add(prod);

                var juego = Juegos[r.Next(Juegos.Length)];
                var pedidas = juego.Select(_ => r.Next(2, 31) * 10).ToArray(); // 20 a 300 por talla

                var operaciones = r.Next(1, 5);
                for (var k = 0; k < operaciones; k++)
                {
                    var procesoId = procesos[r.Next(procesos.Count)];
                    var conEse = hacen.Where(h => h.ProcesoId == procesoId).Select(h => h.ProveedorId).ToList();
                    var proveedorId = conEse.Count > 0 && r.NextDouble() < 0.8 ? conEse[r.Next(conEse.Count)] : proveedores[r.Next(proveedores.Count)];
                    var precio = r.Next(10, 81) * 50L; // $5.00 a $40.00

                    var o = new Orden
                    {
                        Produccion = prod,
                        Np = np++,
                        ProcesoId = procesoId,
                        ProveedorId = proveedorId,
                        PrecioCentavos = precio,
                        PrecioSegundaCentavos = r.NextDouble() < 0.5 ? precio : precio / 2,
                    };
                    for (var t = 0; t < juego.Length; t++)
                        o.Tallas.Add(new OrdenTalla { Talla = juego[t], Posicion = t, Pedidas = pedidas[t] });

                    // Estado al azar: 15% sin enlace · 15% esperando · 15% por agendar · 35% programada · 20% entregada
                    var dado = r.NextDouble();
                    var cita = hoy.AddDays(r.Next(0, 22)).AddHours(r.Next(8, 19));
                    if (dado >= 0.15) { o.Token = await TokenLibre(db, r); o.TokenCreadoEn = DateTime.UtcNow.AddDays(-r.Next(1, 10)); }
                    if (dado >= 0.30)
                    {
                        Contar(o, r);
                        o.RecibidoEn = DateTime.UtcNow.AddHours(-r.Next(1, 200));
                        o.CitaPropuesta = cita;
                        if (r.NextDouble() < 0.3) o.Comentario = "Algunas piezas con detalle.";
                    }
                    if (dado >= 0.45) o.CitaConfirmada = r.NextDouble() < 0.8 ? cita : cita.AddDays(1);
                    if (dado >= 0.80)
                    {
                        // Entregada: la cita fue en las últimas 3 semanas
                        var pasada = hoy.AddDays(-r.Next(1, 22)).AddHours(r.Next(8, 19));
                        o.CitaPropuesta = pasada;
                        o.CitaConfirmada = pasada;
                        o.DeclaradoJson = JsonSerializer.Serialize(
                            o.Tallas.Select(t => new TallaDto(t.Talla, t.Pedidas, t.Primeras, t.Segundas)), new JsonSerializerOptions(JsonSerializerDefaults.Web));
                        if (r.NextDouble() < 0.3) foreach (var t in o.Tallas) t.Primeras = Math.Max(0, t.Primeras - r.Next(0, 4));
                        o.EntregadaEn = DateTime.SpecifyKind(pasada.AddHours(6 + 1), DateTimeKind.Utc); // hora de México → UTC, una hora después
                        o.NotaEntrega = r.NextDouble() < 0.2 ? "Faltaron algunas piezas." : null;
                    }
                    db.Ordenes.Add(o);
                    cuantas++;
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { producciones = n, ordenes = cuantas });
        });
    }

    // Conteo del proveedor: casi todo de primera, alguna segunda y algún faltante
    private static void Contar(Orden o, Random r)
    {
        foreach (var t in o.Tallas)
        {
            var segundas = r.NextDouble() < 0.3 ? r.Next(1, 4) : 0;
            var faltan = r.NextDouble() < 0.15 ? r.Next(1, 6) : 0;
            t.Segundas = Math.Min(segundas, t.Pedidas);
            t.Primeras = Math.Max(0, t.Pedidas - t.Segundas - faltan);
        }
    }

    // Igual que el de OrdenesApi, pero sin ir a la base por cada uno (con 10 caracteres al azar no se repiten)
    private static Task<string> TokenLibre(AppDb db, Random r)
    {
        const string alfabeto = "23456789abcdefghjkmnpqrstuvwxyz";
        var s = new string(Enumerable.Range(0, 10).Select(_ => alfabeto[r.Next(alfabeto.Length)]).ToArray());
        return Task.FromResult($"{s[..5]}-{s[5..]}");
    }
}