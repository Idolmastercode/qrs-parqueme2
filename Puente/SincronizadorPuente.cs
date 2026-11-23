using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using qrs_back.Data;
using qrs_back.Endpoints;

namespace qrs_back.Puente;

/// <summary>Lo que va en appsettings.json → "Puente".</summary>
public class OpcionesPuente
{
    /// <summary>Dirección del puente en IONOS, p. ej. https://pedidos.cartoonia.mx. Vacío = sin sincronizar.</summary>
    public string Url { get; set; } = "";

    /// <summary>La misma clave que clave_puente en api/config.php del puente.</summary>
    public string Clave { get; set; } = "";

    /// <summary>Cada cuántos segundos se sincroniza.</summary>
    public int CadaSegundos { get; set; } = 10;
}

// Lo que se sube: solo lo que el proveedor necesita ver (sin precios ni cliente)
public record TallaPublica(string Talla, int Pedidas, int Primeras, int Segundas);
public record OrdenPublica(
    string Token, int OrdenId, string Empresa, long Op, long Operacion, string Modelo, string Proceso, string Proveedor,
    List<TallaPublica> Tallas, string Comentario, string? CitaPropuesta, string? CitaConfirmada, string? RecibidoEn);

// Lo que se baja: lo que mandó el proveedor desde su celular
public record ConteoPublico(string Talla, int Primeras, int Segundas);
public record Envio(long Id, string Token, int OrdenId, List<ConteoPublico> Tallas, string Comentario, string CitaPropuesta, string RecibidoEn);
public record RespuestaEnvios(List<Envio> Envios);
public record RespuestaSubida(int Ordenes, int Borradas, int Conservadas);

/// <summary>
/// Sincroniza la oficina con la página pública de los QR (el "puente" en IONOS).
/// Cada vuelta (10 s):
///   1. BAJA los conteos que mandaron los proveedores, los guarda aquí y le confirma al puente.
///   2. SUBE la lista de órdenes con enlace (solo si cambió, o cada 5 minutos por si acaso).
/// Si no hay internet o el puente falla, lo intenta en la siguiente vuelta; nada se pierde
/// porque los conteos se quedan en el puente hasta que esta oficina confirma que los guardó.
/// </summary>
public class SincronizadorPuente(
    IServiceScopeFactory scopes,
    IHttpClientFactory http,
    IOptions<OpcionesPuente> opciones,
    ILogger<SincronizadorPuente> log) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string _ultimaHuella = "";
    private DateTime _ultimaSubida = DateTime.MinValue;
    private string? _ultimoError;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var o = opciones.Value;
        if (string.IsNullOrWhiteSpace(o.Url) || string.IsNullOrWhiteSpace(o.Clave))
        {
            log.LogInformation("Puente: sin configurar (Puente:Url / Puente:Clave en appsettings.json). Los QR no se sincronizan.");
            return;
        }
        log.LogInformation("Puente: sincronizando con {Url} cada {N} s", o.Url, o.CadaSegundos);

        using var reloj = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(3, o.CadaSegundos)));
        do
        {
            try
            {
                var cliente = http.CreateClient("puente");
                cliente.BaseAddress = new Uri(o.Url.TrimEnd('/') + "/");
                cliente.DefaultRequestHeaders.Remove("X-Clave-Puente");
                cliente.DefaultRequestHeaders.Add("X-Clave-Puente", o.Clave);

                await BajarEnvios(cliente, ct);
                await SubirOrdenes(cliente, ct);

                if (_ultimoError is not null) log.LogInformation("Puente: conexión recuperada");
                _ultimoError = null;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                // Solo se avisa cuando cambia el error, para no llenar la consola cada 10 s
                if (e.Message != _ultimoError) log.LogWarning("Puente: no se pudo sincronizar ({Error}). Se reintenta solo.", e.Message);
                _ultimoError = e.Message;
            }
        } while (await reloj.WaitForNextTickAsync(ct));
    }

    // =====================================================================
    //  1. Bajar lo que mandaron los proveedores
    // =====================================================================
    private async Task BajarEnvios(HttpClient cliente, CancellationToken ct)
    {
        var r = await cliente.GetAsync("api/puente/envios", ct);
        await Revisar(r, ct);
        var envios = (await r.Content.ReadFromJsonAsync<RespuestaEnvios>(Json, ct))?.Envios ?? [];
        if (envios.Count == 0) return;

        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var guardados = new List<long>();

        foreach (var e in envios.OrderBy(x => x.Id))
        {
            var orden = await db.Ordenes
                .Include(x => x.Tallas)
                .Include(x => x.LotesSegundas)
                .FirstOrDefaultAsync(x => x.Token == e.Token && x.Id == e.OrdenId, ct);

            // Si ya no existe o le quitaron el enlace: se ignora (pero se confirma).
            // Si aquí ya confirmaron la cita mientras el envío venía en camino, se guarda igual su conteo
            // y su propuesta como referencia; la cita confirmada no se toca.
            if (orden is not null)
            {
                foreach (var t in orden.Tallas)
                {
                    var c = e.Tallas.FirstOrDefault(x => string.Equals(x.Talla, t.Talla, StringComparison.OrdinalIgnoreCase));
                    if (c is null || c.Primeras < 0 || c.Segundas < 0 || c.Primeras + c.Segundas > t.Pedidas) continue;
                    t.Primeras = c.Primeras;
                    t.Segundas = c.Segundas;
                }
                orden.Comentario = OrdenesApi.LimpiarComentario(e.Comentario);
                orden.CitaPropuesta = OrdenesApi.LeerCita(e.CitaPropuesta) ?? orden.CitaPropuesta;
                orden.RecibidoEn = DateTime.Parse(e.RecibidoEn, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

                // Igual que en la página: si los precios especiales ya no cuadran con el conteo nuevo, se quitan
                var tallas = orden.Tallas.Select(t => new TallaDto(t.Talla, t.Pedidas, t.Primeras, t.Segundas)).ToList();
                var lotes = orden.LotesSegundas.Select(l => new LoteDto(l.Cantidad, l.PrecioCentavos, l.Talla, l.Motivo)).ToList();
                if (OrdenesApi.RevisarLotes(lotes, tallas) is not null) orden.LotesSegundas.Clear();

                await db.SaveChangesAsync(ct);
                log.LogInformation("Puente: llegó el conteo de la operación {Np}", orden.Np);
            }
            guardados.Add(e.Id);
        }

        var conf = await cliente.PostAsJsonAsync("api/puente/envios/confirmar", new { ids = guardados }, Json, ct);
        await Revisar(conf, ct);
        _ultimaHuella = ""; // lo que se guardó cambia la lista: que se suba en esta misma vuelta
    }

    // =====================================================================
    //  2. Subir las órdenes con enlace
    // =====================================================================
    private async Task SubirOrdenes(HttpClient cliente, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var filas = await db.Ordenes
            .AsNoTracking()
            .Where(x => x.Token != null)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.Token, x.Id, Empresa = x.Produccion.Empresa.Nombre, x.Produccion.Op, x.Np, x.Produccion.Modelo,
                Proceso = x.Proceso.Nombre, Proveedor = x.Proveedor.Nombre,
                Tallas = x.Tallas.OrderBy(t => t.Posicion).Select(t => new TallaPublica(t.Talla, t.Pedidas, t.Primeras, t.Segundas)).ToList(),
                x.Comentario, x.CitaPropuesta, x.CitaConfirmada, x.RecibidoEn,
            })
            .ToListAsync(ct);

        var lista = filas.Select(x => new OrdenPublica(
            x.Token!, x.Id, x.Empresa, x.Op, x.Np, x.Modelo, x.Proceso, x.Proveedor, x.Tallas, x.Comentario ?? "",
            OrdenesApi.Cita(x.CitaPropuesta), OrdenesApi.Cita(x.CitaConfirmada),
            x.RecibidoEn?.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture))).ToList();

        // Solo se sube si algo cambió (o cada 5 minutos, por si el puente perdió sus datos)
        var json = JsonSerializer.Serialize(lista, Json);
        var huella = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        if (huella == _ultimaHuella && DateTime.UtcNow - _ultimaSubida < TimeSpan.FromMinutes(5)) return;

        var r = await cliente.PutAsJsonAsync("api/puente/ordenes", new { ordenes = lista }, Json, ct);
        await Revisar(r, ct);
        _ultimaHuella = huella;
        _ultimaSubida = DateTime.UtcNow;
        var res = await r.Content.ReadFromJsonAsync<RespuestaSubida>(Json, ct);
        log.LogDebug("Puente: {N} órdenes publicadas, {B} quitadas", res?.Ordenes, res?.Borradas);
    }

    private static async Task Revisar(HttpResponseMessage r, CancellationToken ct)
    {
        if (r.IsSuccessStatusCode) return;
        var texto = await r.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"{(int)r.StatusCode} {texto}");
    }
}