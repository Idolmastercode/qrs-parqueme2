namespace qrs_back.Entities;

/// <summary>
/// Un proceso de una producción mandado a UN proveedor con UNA cita (una entrega).
/// Si un proceso se reparte entre proveedores o se entrega en partes, son varias órdenes
/// con el mismo proceso dentro de la misma producción.
///
/// El estado NO se guarda, se calcula:
///   sin Token                                  → Sin enlace
///   Token y sin RecibidoEn                     → Esperando proveedor
///   RecibidoEn y sin CitaConfirmada            → Por agendar
///   CitaConfirmada (antes del día)             → Programada
///   CitaConfirmada (ya es el día o pasó)       → Por recibir
///   EntregadaEn                                → Entregada (cerrada: ya no se edita)
///
/// Importe = primeras × Precio + segundas en lotes × su precio + segundas sin lote × PrecioSegunda.
/// Los faltantes no se pagan. Lo que se paga es lo RECIBIDO (la entrega), no lo que declaró el proveedor.
/// </summary>
public class Orden : IConFechas, IBorrable
{
    public int Id { get; set; }

    public int ProduccionId { get; set; }
    public Produccion Produccion { get; set; } = null!;

    /// <summary>
    /// Número de SAP de esta orden. Único dentro de la empresa (aunque un proceso se reparta,
    /// cada parte lleva su propio NP). Como la empresa vive en la producción, SQLite no puede
    /// forzarlo con un índice: se valida en la API al crear/editar.
    /// </summary>
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

    // ---- Entrega (lo que de verdad llegó y se paga) ----
    /// <summary>Cuándo se registró la entrega. Con esto la orden queda cerrada y ya no se edita.</summary>
    public DateTime? EntregadaEn { get; set; }
    /// <summary>Quién recibió la mercancía.</summary>
    public string? RecibioNombre { get; set; }
    public string? NotaEntrega { get; set; }
    /// <summary>Copia (JSON) de lo que había declarado el proveedor al momento de recibir, para comparar después.</summary>
    public string? DeclaradoJson { get; set; }

    /// <summary>Última vez que se usó "Corregir pedido": se descartó la respuesta del proveedor y tiene que volver a enviarla.</summary>
    public DateTime? PedidoCorregidoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }

    public List<OrdenTalla> Tallas { get; set; } = [];
    public List<LoteSegundas> LotesSegundas { get; set; } = [];
}