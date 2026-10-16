namespace qrs_back.Entities;

/// <summary>
/// Un proceso de una producción mandado a UN proveedor con UNA cita (una entrega).
/// Si un proceso se reparte entre proveedores o se entrega en partes, son varias órdenes
/// con el mismo proceso dentro de la misma producción.
///
/// El estado NO se guarda, se calcula:
///   sin Token                         → Creada
///   Token y sin RecibidoEn            → Enviada
///   RecibidoEn y sin CitaConfirmada   → Recibida
///   CitaConfirmada en el futuro       → Programada
///   CitaConfirmada ya pasó            → Cerrada (si se reagenda, se reabre sola)
///
/// Importe = primeras × Precio + segundas en lotes × su precio + segundas sin lote × PrecioSegunda.
/// Los faltantes no se pagan.
/// </summary>
public class Orden : IConFechas, IBorrable
{
    public int Id { get; set; }

    public int ProduccionId { get; set; }
    public Produccion Produccion { get; set; } = null!;

    /// <summary>Número de SAP de esta orden. No es único en la base porque una orden de SAP puede repartirse entre varios proveedores.</summary>
    public long Np { get; set; }

    public int ProcesoId { get; set; }
    public Proceso Proceso { get; set; } = null!;

    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    /// <summary>Precio por pieza de primera, en centavos ($18.50 → 1850).</summary>
    public long PrecioCentavos { get; set; }

    /// <summary>Precio por defecto para las segundas que no estén en un lote, en centavos.</summary>
    public long PrecioSegundaCentavos { get; set; }

    /// <summary>Comentario general (lo escribe el proveedor o ustedes).</summary>
    public string? Comentario { get; set; }

    // ---- Enlace / QR (uno por orden, reutilizable hasta que hay cita confirmada) ----
    /// <summary>Código al azar del enlace. Null = todavía sin QR (o desactivado).</summary>
    public string? Token { get; set; }
    public DateTime? TokenCreadoEn { get; set; }
    /// <summary>Última vez que el proveedor envió el formulario.</summary>
    public DateTime? RecibidoEn { get; set; }

    // ---- Cita (hora local de México) ----
    public DateTime? CitaPropuesta { get; set; }
    public DateTime? CitaConfirmada { get; set; }

    public DateTime? EliminadoEn { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }

    public List<OrdenTalla> Tallas { get; set; } = [];
    public List<LoteSegundas> LotesSegundas { get; set; } = [];
}
