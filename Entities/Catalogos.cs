namespace qrs_back.Entities;

/// <summary>American Polo, Cartoonia, Cotton &amp; Print, MJCO. La OP y el NP son únicos dentro de cada una.</summary>
public class Empresa : Catalogo { }

/// <summary>Tiendas: Liverpool, Coppel…</summary>
public class Cliente : Catalogo { }

/// <summary>Maquileros. Siempre se llaman "proveedor" en todo el sistema.</summary>
public class Proveedor : Catalogo { }

/// <summary>Bordado, Estampado, Lavandería…</summary>
public class Proceso : Catalogo { }
