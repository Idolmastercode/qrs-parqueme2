namespace qrs_back.Entities;

/// <summary>
/// Bitácora de lo delicado: registrar o reabrir una entrega, corregir un pedido.
/// Guarda quién, cuándo, por qué y el detalle (JSON) de lo que había y lo que quedó.
/// </summary>
public class CambioOrden
{
    public int Id { get; set; }
    public int OrdenId { get; set; }

    /// <summary>"entrega", "reapertura", "corregir-pedido"…</summary>
    public string Accion { get; set; } = "";
    public string? Quien { get; set; }
    public string? Motivo { get; set; }
    public string? Detalle { get; set; }

    /// <summary>UTC (termina en "En").</summary>
    public DateTime RegistradoEn { get; set; }
}