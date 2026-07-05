using Microsoft.EntityFrameworkCore;
using EkoWebApi.Data.Entities;

namespace EkoWebApi.Data;

/// <summary>
/// Mappar mot den redan existerande EkoWeb-databasen (skapad och underhållen
/// av ett annat system). Denna context används bara för att läsa/skriva mot
/// befintliga tabeller - EnsureCreated/Migrate ska ALDRIG köras mot den här
/// databasen, eftersom den innehåller riktig produktionsdata och schemat ägs
/// av EkoWeb, inte av det här API:et.
/// </summary>
public class EkoWebDbContext : DbContext
{
    public EkoWebDbContext(DbContextOptions<EkoWebDbContext> options) : base(options)
    {
    }

    public DbSet<Institut> Institut => Set<Institut>();

    public DbSet<Kontotyp> Kontotyper => Set<Kontotyp>();

    public DbSet<Konto> Konton => Set<Konto>();

    public DbSet<Kategori> Kategorier => Set<Kategori>();

    public DbSet<Ekonomi> Ekonomier => Set<Ekonomi>();

    public DbSet<Anvandare> Anvandare => Set<Anvandare>();

    public DbSet<Anvandarroll> Anvandarroller => Set<Anvandarroll>();

    public DbSet<EkonomiAnvandare> EkonomiAnvandare => Set<EkonomiAnvandare>();

    public DbSet<KontoAnvandare> KontoAnvandare => Set<KontoAnvandare>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Institut>().ToTable("Institut");

        modelBuilder.Entity<Kontotyp>(entity =>
        {
            entity.ToTable("Kontotyp");
            entity.Property(e => e.Externt).HasConversion<short>();
        });

        modelBuilder.Entity<Konto>(entity =>
        {
            entity.ToTable("Konto");
            entity.HasOne(k => k.Kontotyp).WithMany().HasForeignKey(k => k.KontotypId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(k => k.Institut).WithMany().HasForeignKey(k => k.InstitutId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Kategori>(entity =>
        {
            entity.ToTable("Kategori");
            entity.HasOne(k => k.Foralder).WithMany().HasForeignKey(k => k.ForalderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(k => k.Ekonomi).WithMany().HasForeignKey(k => k.EkonomiId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ekonomi>(entity =>
        {
            entity.ToTable("Ekonomi");
            entity.HasOne(e => e.EkonomiAgare).WithMany().HasForeignKey(e => e.EkonomiAgareId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Anvandare>().ToTable("Anvandare");

        modelBuilder.Entity<Anvandarroll>().ToTable("Anvandarroll");

        modelBuilder.Entity<EkonomiAnvandare>(entity =>
        {
            entity.ToTable("Ekonomi_Anvandare");
            entity.Property(e => e.Andel).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Ekonomi).WithMany(e => e.Anvandare).HasForeignKey(e => e.EkonomiId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Anvandare).WithMany().HasForeignKey(e => e.AnvadareId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Anvandarroll).WithMany().HasForeignKey(e => e.AnvandarrollId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KontoAnvandare>(entity =>
        {
            entity.ToTable("Konto_Anvadare");
            entity.Property(e => e.AndelProcent).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Konto).WithMany(k => k.Anvandare).HasForeignKey(e => e.KontoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Anvandare).WithMany().HasForeignKey(e => e.AnvandareId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
