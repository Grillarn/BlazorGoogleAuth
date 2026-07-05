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
