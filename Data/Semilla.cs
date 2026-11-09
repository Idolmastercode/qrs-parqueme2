using qrs_back.Entities;

namespace qrs_back.Data;

/// <summary>
/// Datos iniciales que van dentro de las migraciones.
/// Los Id son fijos: no los cambies ni reordenes, porque EF los usa para saber qué fila es cuál.
/// Para agregar uno nuevo, usa el siguiente número al final (o mejor, dalo de alta desde el panel).
/// </summary>
public static class Semilla
{
    public static readonly Cliente[] Clientes =
    [
        new() { Id = 1,  Nombre = "Aéropostale" },
        new() { Id = 2,  Nombre = "Andrea" },
        new() { Id = 3,  Nombre = "C&A" },
        new() { Id = 4,  Nombre = "CCP" },            // Cuidado con el Perro
        new() { Id = 5,  Nombre = "Chedraui" },
        new() { Id = 6,  Nombre = "Control" },
        new() { Id = 7,  Nombre = "Coppel" },
        new() { Id = 8,  Nombre = "ImportaCop" },     // Coppel importación
        new() { Id = 9,  Nombre = "Liverpool" },
        new() { Id = 10, Nombre = "LOB" },
        new() { Id = 11, Nombre = "Malayerba" },
        new() { Id = 12, Nombre = "Nautica" },
        new() { Id = 13, Nombre = "OfficeMax" },
        new() { Id = 14, Nombre = "Optima" },
        new() { Id = 15, Nombre = "Palacio de Hierro" },
        new() { Id = 16, Nombre = "Shasa" },
        new() { Id = 17, Nombre = "Suburbia" },
        new() { Id = 18, Nombre = "Walmart" },
    ];

    public static readonly Proceso[] Procesos =
    [
        new() { Id = 1, Nombre = "Bordado" },
        new() { Id = 2, Nombre = "Confección" },
        new() { Id = 3, Nombre = "Estampado" },
        new() { Id = 4, Nombre = "Piedra" },
    ];

    /// <summary>Lista de maquileros (nov 2026). El orden es el de la hoja original.</summary>
    public static readonly Proveedor[] Proveedores =
    [
        new() { Id = 1,  Nombre = "Mara" },
        new() { Id = 2,  Nombre = "Lalo Silverato" },
        new() { Id = 3,  Nombre = "Pulpo" },
        new() { Id = 4,  Nombre = "José Onofre" },
        new() { Id = 5,  Nombre = "Karina" },
        new() { Id = 6,  Nombre = "Eva" },
        new() { Id = 7,  Nombre = "Tamayo" },
        new() { Id = 8,  Nombre = "José Contreras" },
        new() { Id = 9,  Nombre = "Fabián" },
        new() { Id = 10, Nombre = "Lety" },
        new() { Id = 11, Nombre = "Celia" },
        new() { Id = 12, Nombre = "Manuel" },
        new() { Id = 13, Nombre = "Luis" },
        new() { Id = 14, Nombre = "Javier" },
        new() { Id = 15, Nombre = "Laura" },
        new() { Id = 16, Nombre = "María Ahumada" },
        new() { Id = 17, Nombre = "Juan Sosa" },
        new() { Id = 18, Nombre = "Lulú Arenas" },
        new() { Id = 19, Nombre = "Blanca Mendoza" },
        new() { Id = 20, Nombre = "Alejandro" },
        new() { Id = 21, Nombre = "Raquel" },
        new() { Id = 22, Nombre = "Juan Lemus" },
        new() { Id = 23, Nombre = "Miguel García" },
        new() { Id = 24, Nombre = "Juan Calderón" },
        new() { Id = 25, Nombre = "Artz" },
        new() { Id = 26, Nombre = "Paw Printex" },
    ];

    public static readonly ProveedorProceso[] ProveedorProcesos =
    [
        new() { ProveedorId = 1,  ProcesoId = 1 }, // Mara → Bordado
        new() { ProveedorId = 2,  ProcesoId = 4 }, // Lalo Silverato → Piedra
        new() { ProveedorId = 3,  ProcesoId = 3 }, // Pulpo → Estampado
        new() { ProveedorId = 4,  ProcesoId = 2 }, // José Onofre → Confección
        new() { ProveedorId = 5,  ProcesoId = 2 }, // Karina → Confección
        new() { ProveedorId = 6,  ProcesoId = 2 }, // Eva → Confección
        new() { ProveedorId = 7,  ProcesoId = 2 }, // Tamayo → Confección
        new() { ProveedorId = 8,  ProcesoId = 2 }, // José Contreras → Confección
        new() { ProveedorId = 9,  ProcesoId = 2 }, // Fabián → Confección
        new() { ProveedorId = 10, ProcesoId = 2 }, // Lety → Confección
        new() { ProveedorId = 11, ProcesoId = 2 }, // Celia → Confección
        new() { ProveedorId = 12, ProcesoId = 2 }, // Manuel → Confección
        new() { ProveedorId = 13, ProcesoId = 2 }, // Luis → Confección
        new() { ProveedorId = 14, ProcesoId = 2 }, // Javier → Confección
        new() { ProveedorId = 15, ProcesoId = 2 }, // Laura → Confección
        new() { ProveedorId = 16, ProcesoId = 2 }, // María Ahumada → Confección
        new() { ProveedorId = 17, ProcesoId = 2 }, // Juan Sosa → Confección
        new() { ProveedorId = 18, ProcesoId = 2 }, // Lulú Arenas → Confección
        new() { ProveedorId = 19, ProcesoId = 2 }, // Blanca Mendoza → Confección
        new() { ProveedorId = 20, ProcesoId = 2 }, // Alejandro → Confección
        new() { ProveedorId = 21, ProcesoId = 2 }, // Raquel → Confección
        new() { ProveedorId = 22, ProcesoId = 1 }, // Juan Lemus → Bordado
        new() { ProveedorId = 23, ProcesoId = 1 }, // Miguel García → Bordado
        new() { ProveedorId = 24, ProcesoId = 1 }, // Juan Calderón → Bordado
        new() { ProveedorId = 25, ProcesoId = 3 }, // Artz → Estampado
        new() { ProveedorId = 26, ProcesoId = 3 }, // Paw Printex → Estampado
    ];
}