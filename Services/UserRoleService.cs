using Microsoft.EntityFrameworkCore;
using BlazorGoogleAuth.Data;
using BlazorGoogleAuth.Data.Entities;

namespace BlazorGoogleAuth.Services;

public interface IUserRoleService
{
    /// <summary>
    /// Hämtar roller för en inloggad e-post. Om användaren inte finns i databasen
    /// sedan tidigare skapas den (provisioneras) automatiskt med grundrollen "User".
    /// </summary>
    Task<IReadOnlyList<string>> GetOrProvisionRolesAsync(string email, string? displayName);

    Task<List<AppUser>> GetAllUsersAsync();

    /// <summary>
    /// Tilldelar en användare en roll. Returnerar false om rolltypen inte
    /// finns bland de definierade rolltyperna (se Roles-tabellen).
    /// </summary>
    Task<bool> AddRoleAsync(int userId, string role);

    Task RemoveRoleAsync(int userId, string role);

    // --- Hantering av själva rolltyperna (t.ex. skapa en ny roll "Support") ---

    Task<List<Role>> GetAllRoleDefinitionsAsync();

    /// <summary>
    /// Skapar en ny rolltyp. Returnerar false om namnet redan finns
    /// (skiftlägesokänsligt) eller är tomt.
    /// </summary>
    Task<bool> CreateRoleDefinitionAsync(string name);

    /// <summary>
    /// Tar bort en rolltyp. Misslyckas om rollen fortfarande är tilldelad
    /// till minst en användare, för att undvika "spöktilldelningar".
    /// </summary>
    Task<(bool Success, string? Error)> DeleteRoleDefinitionAsync(string name);
}

public class DbUserRoleService : IUserRoleService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public DbUserRoleService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<string>> GetOrProvisionRolesAsync(string email, string? displayName)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            user = new AppUser
            {
                Email = email,
                DisplayName = displayName,
                CreatedAtUtc = DateTime.UtcNow,
            };

            // Alla nya användare får grundrollen "User".
            user.Roles.Add(new UserRole { Role = "User" });

            // Engångs-bootstrap: e-postadresser listade i appsettings.json under
            // "Authorization:AdminEmails" blir automatiskt Admin *första* gången
            // de loggar in. Efter det hanteras roller helt via databasen/admin-UI:t,
            // så du kan lika gärna tömma listan i appsettings.json när du har
            // minst en admin i databasen.
            var seedAdmins = _configuration.GetSection("Authorization:AdminEmails").Get<string[]>() ?? [];
            if (seedAdmins.Contains(email, StringComparer.OrdinalIgnoreCase))
            {
                user.Roles.Add(new UserRole { Role = "Admin" });
            }

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else if (!string.IsNullOrWhiteSpace(displayName) && user.DisplayName != displayName)
        {
            // Håll namnet uppdaterat om det ändras på Google-kontot.
            user.DisplayName = displayName;
            await _db.SaveChangesAsync();
        }

        return user.Roles.Select(r => r.Role).ToList();
    }

    public async Task<List<AppUser>> GetAllUsersAsync()
    {
        return await _db.Users
            .Include(u => u.Roles)
            .OrderBy(u => u.Email)
            .ToListAsync();
    }

    public async Task<bool> AddRoleAsync(int userId, string role)
    {
        var roleDefinitionExists = await _db.Roles.AnyAsync(r => r.Name == role);
        if (!roleDefinitionExists)
        {
            return false;
        }

        var alreadyHasRole = await _db.UserRoles
            .AnyAsync(r => r.AppUserId == userId && r.Role == role);

        if (!alreadyHasRole)
        {
            _db.UserRoles.Add(new UserRole { AppUserId = userId, Role = role });
            await _db.SaveChangesAsync();
        }

        return true;
    }

    public async Task RemoveRoleAsync(int userId, string role)
    {
        var existing = await _db.UserRoles
            .FirstOrDefaultAsync(r => r.AppUserId == userId && r.Role == role);

        if (existing is not null)
        {
            _db.UserRoles.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<Role>> GetAllRoleDefinitionsAsync()
    {
        return await _db.Roles
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<bool> CreateRoleDefinitionAsync(string name)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var exists = await _db.Roles.AnyAsync(r => r.Name.ToLower() == name.ToLower());
        if (exists)
        {
            return false;
        }

        _db.Roles.Add(new Role { Name = name, CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error)> DeleteRoleDefinitionAsync(string name)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == name);
        if (role is null)
        {
            return (false, "Rolltypen hittades inte.");
        }

        var inUse = await _db.UserRoles.AnyAsync(ur => ur.Role == name);
        if (inUse)
        {
            return (false, $"Rolltypen \"{name}\" är tilldelad minst en användare och kan inte tas bort.");
        }

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
