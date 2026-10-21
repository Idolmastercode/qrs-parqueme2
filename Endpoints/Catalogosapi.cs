using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using qrs_back.Data;
using qrs_back.Entities;

namespace qrs_back.Endpoints;

public record CatalogoDto(int Id, string Nombre, bool Activo);

/// <summary>Cuerpo para crear o editar. En PUT, lo que venga null no se toca.</summary>
public record GuardarCatalogo(string? Nombre, bool? Activo);

public static partial class CatalogosApi
{
    public static void MapCatalogos(this IEndpointRouteBuilder app)
    {
        // Todo junto, con la misma forma que MOCK_CATALOGOS del front: una sola llamada al abrir el panel.
        // Incluye los inactivos para que las órdenes viejas sigan mostrando su nombre;
        // el front solo ofrece los activos en las listas para elegir.
        app.MapGet("/api/catalogos", async (AppDb db) => new
        {
            empresas = await Lista(db.Empresas),
            clientes = await Lista(db.Clientes),
            proveedores = await Lista(db.Proveedores),
            procesos = await Lista(db.Procesos),
        });

        // Las empresas no se editan desde aquí: son 4 fijas y vienen en la semilla.
        Mapear<Cliente>(app, "clientes", "cliente", db => db.Clientes);
        Mapear<Proveedor>(app, "proveedores", "proveedor", db => db.Proveedores);
        Mapear<Proceso>(app, "procesos", "proceso", db => db.Procesos);
    }

    // GET /api/{ruta}       lista
    // POST /api/{ruta}      { nombre }            → 201, 400 si viene vacío, 409 si ya existe
    // PUT /api/{ruta}/{id}  { nombre?, activo? }  → 200, 404, 409
    // No hay DELETE: para quitarlo de las listas se manda activo:false.
    private static void Mapear<T>(IEndpointRouteBuilder app, string ruta, string singular, Func<AppDb, DbSet<T>> tabla)
        where T : Catalogo, new()
    {
        var g = app.MapGroup($"/api/{ruta}");

        g.MapGet("/", (AppDb db) => Lista(tabla(db)));

        g.MapPost("/", async (GuardarCatalogo cuerpo, AppDb db) =>
        {
            var nombre = Limpiar(cuerpo.Nombre);
            if (nombre is null) return NombreInvalido();

            // La columna es NOCASE: "coppel" choca con "Coppel"
            if (await tabla(db).AnyAsync(x => x.Nombre == nombre))
                return Results.Conflict(new { error = $"Ya existe el {singular} {nombre}." });

            var item = new T { Nombre = nombre, Activo = cuerpo.Activo ?? true };
            tabla(db).Add(item);
            await db.SaveChangesAsync();
            return Results.Created($"/api/{ruta}/{item.Id}", Dto(item));
        });

        g.MapPut("/{id:int}", async (int id, GuardarCatalogo cuerpo, AppDb db) =>
        {
            var item = await tabla(db).FindAsync(id);
            if (item is null) return Results.NotFound();

            if (cuerpo.Nombre is not null)
            {
                var nombre = Limpiar(cuerpo.Nombre);
                if (nombre is null) return NombreInvalido();
                if (await tabla(db).AnyAsync(x => x.Id != id && x.Nombre == nombre))
                    return Results.Conflict(new { error = $"Ya existe el {singular} {nombre}." });
                item.Nombre = nombre;
            }
            if (cuerpo.Activo is bool activo) item.Activo = activo;

            await db.SaveChangesAsync();
            return Results.Ok(Dto(item));
        });
    }

    private static Task<List<CatalogoDto>> Lista<T>(IQueryable<T> q) where T : Catalogo =>
        q.OrderBy(x => x.Nombre).Select(x => new CatalogoDto(x.Id, x.Nombre, x.Activo)).ToListAsync();

    private static CatalogoDto Dto(Catalogo x) => new(x.Id, x.Nombre, x.Activo);

    private static IResult NombreInvalido() =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["nombre"] = ["Escribe un nombre de hasta 80 caracteres."],
        });

    /// <summary>Quita espacios de más ("  Palacio   de Hierro " → "Palacio de Hierro"). Null si queda vacío o muy largo.</summary>
    private static string? Limpiar(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var n = Espacios().Replace(s.Trim(), " ");
        return n.Length <= 80 ? n : null;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();
}