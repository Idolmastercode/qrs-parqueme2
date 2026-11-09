namespace qrs_back.Entities;

/// <summary>American Polo, Cartoonia, Cotton &amp; Print, MJCO. La OP y el NP son únicos dentro de cada una.</summary>
public class Empresa : Catalogo { }

/// <summary>Tiendas: Liverpool, Coppel…</summary>
public class Cliente : Catalogo { }

/// <summary>Maquileros. Siempre se llaman "proveedor" en todo el sistema.</summary>
public class Proveedor : Catalogo
{
    /// <summary>Procesos que hace (Mara → Bordado). Sirve para ofrecer solo a los que hacen ese proceso al crear una orden.</summary>
    public List<ProveedorProceso> Procesos { get; set; } = [];
}

/// <summary>Bordado, Estampado, Confección, Piedra…</summary>
public class Proceso : Catalogo { }

/// <summary>Qué proveedor hace qué proceso. Uno puede hacer varios.</summary>
public class ProveedorProceso
{
    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;
    public int ProcesoId { get; set; }
    public Proceso Proceso { get; set; } = null!;
}