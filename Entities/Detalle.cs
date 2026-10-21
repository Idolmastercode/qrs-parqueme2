namespace qrs_back.Entities;

/// <summary>
/// Desglose por talla de una orden. La talla es texto libre (CH, 30, UNI…);
/// las plantillas son solo ayuda del front. Faltan = Pedidas − Primeras − Segundas (se calcula).
/// </summary>
public class OrdenTalla
{
    public int Id { get; set; }

    public int OrdenId { get; set; }
    public Orden Orden { get; set; } = null!;

    public string Talla { get; set; } = "";
    /// <summary>Orden en que se muestran (CH, M, G… y no alfabético).</summary>
    public int Posicion { get; set; }

    public int Pedidas { get; set; }
    public int Primeras { get; set; }
    public int Segundas { get; set; }
}

/// <summary>
/// Excepciones de precio para segundas: "2 piezas a $10 (bordado picado)", "1 a $0 (dañada)".
/// Las segundas que no estén en un lote se cobran a Orden.PrecioSegundaCentavos.
/// </summary>
public class LoteSegundas
{
    public int Id { get; set; }

    public int OrdenId { get; set; }
    public Orden Orden { get; set; } = null!;

    public int Cantidad { get; set; }
    public long PrecioCentavos { get; set; }
    public string? Talla { get; set; }
    public string? Motivo { get; set; }
}
