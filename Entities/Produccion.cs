namespace qrs_back.Entities;

/// <summary>
/// El "padre": una tanda completa de un modelo, identificada por su OP (orden de producción de SAP).
/// Ej. OP 1000 · NAVIDADGRINCH25 · Liverpool → órdenes NP 50 bordado, NP 51 lavado, NP 52 estampado.
/// Si el modelo se vuelve a pedir, es otra producción con otra OP.
/// </summary>
public class Produccion : IConFechas, IBorrable
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;

    /// <summary>Número de SAP. Único dentro de la empresa.</summary>
    public long Op { get; set; }

    /// <summary>Texto libre; se puede repetir entre producciones (reórdenes).</summary>
    public string Modelo { get; set; } = "";

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    /// <summary>Se marca con un botón: "ya no espero más procesos". Null = en curso.</summary>
    public DateTime? CompletadaEn { get; set; }

    public DateTime? EliminadoEn { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }

    public List<Orden> Ordenes { get; set; } = [];
}
