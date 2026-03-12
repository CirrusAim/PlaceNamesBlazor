using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Data.Entities;

namespace PlaceNamesBlazor.Data;

public class PlaceNamesDbContext : DbContext
{
    public PlaceNamesDbContext(DbContextOptions<PlaceNamesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Fylke> Fylker => Set<Fylke>();
    public DbSet<Kommune> Kommuner => Set<Kommune>();
    public DbSet<Poststed> Poststeder => Set<Poststed>();
    public DbSet<Stempeltype> Stempeltyper => Set<Stempeltype>();
    public DbSet<UnderkategoriStempeltype> UnderkategoriStempeltyper => Set<UnderkategoriStempeltype>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Rapportoer> Rapportoer => Set<Rapportoer>();
    public DbSet<Stempel> Stempler => Set<Stempel>();
    public DbSet<Bruksperiode> Bruksperioder => Set<Bruksperiode>();
    public DbSet<BruksperiodeBilde> BruksperioderBilder => Set<BruksperiodeBilde>();
    public DbSet<Stempelbilde> Stempelbilder => Set<Stempelbilde>();
    public DbSet<Rapporteringshistorikk> Rapporteringshistorikk => Set<Rapporteringshistorikk>();
    public DbSet<RapporteringshistorikkBilde> RapporteringshistorikkBilder => Set<RapporteringshistorikkBilde>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>AuditLog.DetailsJson maps to PostgreSQL jsonb. Table/column names come from [Table]/[Column] on entities.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<AuditLog>(e => e.Property(x => x.DetailsJson).HasColumnType("jsonb"));
    }
}
