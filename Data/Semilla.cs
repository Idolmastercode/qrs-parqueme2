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
        new() { Id = 1,  Nombre = "Aero" },
        new() { Id = 2,  Nombre = "Andrea" },
        new() { Id = 3,  Nombre = "C&A" },
        new() { Id = 4,  Nombre = "CCP" },            // Cuidado con el Perro
        new() { Id = 5,  Nombre = "Chedraui" },
        new() { Id = 6,  Nombre = "Control" },
        new() { Id = 7,  Nombre = "Coppel" },
        new() { Id = 8,  Nombre = "ImportaCop" },     // Coppel importación
        new() { Id = 9,  Nombre = "Liverpool" },
        new() { Id = 10, Nombre = "Lob" },
        new() { Id = 11, Nombre = "Malayerba" },
        new() { Id = 12, Nombre = "Nautica" },
        new() { Id = 13, Nombre = "OfficeMax" },
        new() { Id = 14, Nombre = "Optima" },
        new() { Id = 15, Nombre = "Palacio de Hierro" },
        new() { Id = 16, Nombre = "Shasa" },
        new() { Id = 17, Nombre = "Suburbia" },
        new() { Id = 18, Nombre = "Walmart" },
    ];
}