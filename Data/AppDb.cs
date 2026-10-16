using qrs_back.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace qrs_back.Data;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Proceso> Procesos => Set<Proceso>();
    public DbSet<Produccion> Producciones => Set<Produccion>();
    public DbSet<Orden> Ordenes => Set<Orden>();
    public DbSet<OrdenTalla> OrdenTallas => Set<OrdenTalla>();
    public DbSet<LoteSegundas> LotesSegundas => Set<LoteSegundas>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ---------- Catálogos ----------
        ConfigurarCatalogo<Empresa>(b);
        ConfigurarCatalogo<Cliente>(b);
        ConfigurarCatalogo<Proveedor>(b);
        ConfigurarCatalogo<Proceso>(b);

        // Lo único que sabemos al 100 hoy. Los demás catálogos se capturan desde el panel.
        b.Entity<Empresa>().HasData(
            new Empresa { Id = 1, Nombre = "American Polo" },
            new Empresa { Id = 2, Nombre = "Cartoonia" },
            new Empresa { Id = 3, Nombre = "Cotton & Print" },
            new Empresa { Id = 4, Nombre = "MJCO" }
        );

        // ---------- Producción (padre) ----------
        b.Entity<Produccion>(e =>
        {
            e.Property(x => x.Modelo).HasMaxLength(40).IsRequired();

            // OP única por empresa (sin contar las borradas, para poder recapturar una OP borrada por error)
            e.HasIndex(x => new { x.EmpresaId, x.Op }).IsUnique().HasFilter("EliminadoEn IS NULL");
            e.HasIndex(x => x.Modelo);

            e.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);

            e.ToTable(t => t.HasCheckConstraint("CK_Produccion_Op", "Op > 0"));
            e.HasQueryFilter(x => x.EliminadoEn == null);
        });

        // ---------- Orden de proceso (hijo) ----------
        b.Entity<Orden>(e =>
        {
            e.Property(x => x.Comentario).HasMaxLength(1000);
            e.Property(x => x.Token).HasMaxLength(32);

            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => x.Np);
            e.HasIndex(x => x.CitaConfirmada); // para el calendario

            e.HasOne(x => x.Produccion).WithMany(p => p.Ordenes).HasForeignKey(x => x.ProduccionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Proceso).WithMany().HasForeignKey(x => x.ProcesoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.ProveedorId).OnDelete(DeleteBehavior.Restrict);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Orden_Np", "Np > 0");
                t.HasCheckConstraint("CK_Orden_Precios", "PrecioCentavos >= 0 AND PrecioSegundaCentavos >= 0");
            });

            // Si se borra la producción, sus órdenes también desaparecen de la vista
            e.HasQueryFilter(x => x.EliminadoEn == null && x.Produccion.EliminadoEn == null);
        });

        // ---------- Tallas ----------
        b.Entity<OrdenTalla>(e =>
        {
            e.Property(x => x.Talla).HasMaxLength(10).IsRequired();
            e.HasIndex(x => new { x.OrdenId, x.Talla }).IsUnique();
            e.HasOne(x => x.Orden).WithMany(o => o.Tallas).HasForeignKey(x => x.OrdenId).OnDelete(DeleteBehavior.Cascade);
            // Solo no negativos. "1ras + 2das ≤ pedidas" se valida en la API (más fácil de ajustar si el negocio lo pide).
            e.ToTable(t => t.HasCheckConstraint("CK_OrdenTalla_Cantidades", "Pedidas >= 0 AND Primeras >= 0 AND Segundas >= 0"));
        });

        // ---------- Lotes de segundas ----------
        b.Entity<LoteSegundas>(e =>
        {
            e.Property(x => x.Motivo).HasMaxLength(200);
            e.HasOne(x => x.Orden).WithMany(o => o.LotesSegundas).HasForeignKey(x => x.OrdenId).OnDelete(DeleteBehavior.Cascade);
            e.ToTable(t => t.HasCheckConstraint("CK_LoteSegundas", "Cantidad > 0 AND PrecioCentavos >= 0"));
        });

        // ---------- Fechas UTC ----------
        // SQLite guarda fechas como texto sin zona. Al leer, marcamos como UTC todo lo que termina en "En"
        // para que el JSON salga con "Z" y el front lo convierta bien a hora de México.
        // Las citas (CitaPropuesta / CitaConfirmada) se quedan como hora local tal cual.
        foreach (var entidad in b.Model.GetEntityTypes())
        foreach (var prop in entidad.GetProperties().Where(p => p.Name.EndsWith("En", StringComparison.Ordinal)))
        {
            if (prop.ClrType == typeof(DateTime)) prop.SetValueConverter(Utc);
            else if (prop.ClrType == typeof(DateTime?)) prop.SetValueConverter(UtcNullable);
        }
    }

    private static void ConfigurarCatalogo<T>(ModelBuilder b) where T : Catalogo
    {
        b.Entity<T>(e =>
        {
            // NOCASE: "Coppel" y "coppel" cuentan como el mismo nombre
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired().UseCollation("NOCASE");
            e.HasIndex(x => x.Nombre).IsUnique();
        });
    }

    private static readonly ValueConverter<DateTime, DateTime> Utc =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> UtcNullable =
        new(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

    // ---------- CreadoEn / ActualizadoEn automáticos ----------
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SellarFechas();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SellarFechas();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SellarFechas()
    {
        var ahora = DateTime.UtcNow;
        foreach (var entrada in ChangeTracker.Entries<IConFechas>())
        {
            if (entrada.State == EntityState.Added)
            {
                entrada.Entity.CreadoEn = ahora;
                entrada.Entity.ActualizadoEn = ahora;
            }
            else if (entrada.State == EntityState.Modified)
            {
                entrada.Entity.ActualizadoEn = ahora;
            }
        }
    }
}
