using Microsoft.EntityFrameworkCore;
using BlazorGoogleAuth.Data.Entities;

namespace BlazorGoogleAuth.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            // Måste ha en begränsad längd för att kunna indexeras i SQL Server
            // (nvarchar(max) kan inte vara nyckelkolumn i ett index).
            entity.Property(u => u.GoogleId).HasMaxLength(200);

            // Tillåter många NULL (t.ex. manuellt skapade användare som inte
            // loggat in än) men förhindrar att samma Google-konto kopplas
            // till två rader.
            entity.HasIndex(u => u.GoogleId).IsUnique().HasFilter("[GoogleId] IS NOT NULL");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            // En användare kan inte ha samma roll två gånger.
            entity.HasIndex(ur => new { ur.AppUserId, ur.Role }).IsUnique();

            entity.HasOne(ur => ur.AppUser)
                  .WithMany(u => u.Roles)
                  .HasForeignKey(ur => ur.AppUserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
        });
    }
}
