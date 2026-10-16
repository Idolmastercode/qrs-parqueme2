namespace qrs_back.Entities;

/// <summary>
/// Catálogo simple: empresas, clientes, proveedores y procesos.
/// "Activo" permite ocultar un registro de las listas sin borrarlo
/// (sigue apareciendo en las órdenes viejas que lo usan).
/// </summary>
public abstract class Catalogo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; } = true;
}

/// <summary>
/// CreadoEn / ActualizadoEn se llenan solos al guardar (ver AppDb.SellarFechas).
/// Convención: toda fecha cuyo nombre termina en "En" se guarda en UTC.
/// Las citas NO terminan en "En" porque son hora local de México (lo que se ve en el reloj).
/// </summary>
public interface IConFechas
{
    DateTime CreadoEn { get; set; }
    DateTime ActualizadoEn { get; set; }
}

/// <summary>Borrado lógico: se oculta de todas las consultas pero queda en la base.</summary>
public interface IBorrable
{
    DateTime? EliminadoEn { get; set; }
}
