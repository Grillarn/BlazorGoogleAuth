using Microsoft.EntityFrameworkCore;
using BlazorGoogleAuth.Data;
using BlazorGoogleAuth.Data.Entities;

namespace BlazorGoogleAuth.Services;

/// <summary>
/// De profilfält vi hämtar ut ur Google-inloggningens claims. Övriga fält än
/// Email och DisplayName är valfria eftersom exakt vilka claims Google skickar
/// kan variera.
/// </summary>
public record GoogleProfile(
    string Email,
    string? DisplayName,
    string? GoogleId,
    string? GivenName,
    string? Surname,
    string? PictureUrl);

public interface IUserRoleService
{
    /// <summary>
    /// Hämtar roller för en inloggad användare. Om användaren inte finns i databasen
    /// sedan tidigare skapas den (provisioneras) automatiskt med grundrollen "User".
    /// </summary>
    Task<IReadOnlyList<string>> GetOrProvisionRolesAsync(GoogleProfile profile);

    Task<List<AppUser>> GetAllUsersAsync();

    /// <summary>
    /// Skapar en användare manuellt, utan att personen behöver ha loggat in
    /// via Google först. Praktiskt för att förbereda roller åt någon i förväg -
    /// när de sedan loggar in med samma e-postadress kopplas de automatiskt
    /// till den här posten istället för att en ny skapas.
    /// Returnerar false om e-postadressen redan finns.
    /// </summary>
    Task<(bool Success, string? Error)> CreateUserAsync(string email, string? displayName);

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
    Task<bool> CreateRoleDefinitionAsync(string name, string? description = null);

    /// <summary>
    /// Uppdaterar beskrivningen för en befintlig rolltyp.
    /// </summary>
    Task UpdateRoleDescriptionAsync(string name, string? description);

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

    public async Task<IReadOnlyList<string>> GetOrProvisionRolesAsync(GoogleProfile profile)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == profile.Email);

        if (user is null)
        {
            user = new AppUser
            {
                Email = profile.Email,
                DisplayName = profile.DisplayName,
                GoogleId = profile.GoogleId,
                GivenName = profile.GivenName,
                Surname = profile.Surname,
                PictureUrl = profile.PictureUrl,
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
            if (seedAdmins.Contains(profile.Email, StringComparer.OrdinalIgnoreCase))
            {
                user.Roles.Add(new UserRole { Role = "Admin" });
            }

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else
        {
            // Håll profilfälten uppdaterade om de ändras på Google-kontot.
            var changed = false;

            if (!string.IsNullOrWhiteSpace(profile.DisplayName) && user.DisplayName != profile.DisplayName)
            {
                user.DisplayName = profile.DisplayName;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(profile.GoogleId) && user.GoogleId != profile.GoogleId)
            {
                user.GoogleId = profile.GoogleId;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(profile.GivenName) && user.GivenName != profile.GivenName)
            {
                user.GivenName = profile.GivenName;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(profile.Surname) && user.Surname != profile.Surname)
            {
                user.Surname = profile.Surname;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(profile.PictureUrl) && user.PictureUrl != profile.PictureUrl)
            {
                user.PictureUrl = profile.PictureUrl;
                changed = true;
            }

            if (changed)
            {
                await _db.SaveChangesAsync();
            }
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

    public async Task<(bool Success, string? Error)> CreateUserAsync(string email, string? displayName)
    {
        email = email.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "Ange en e-postadress.");
        }

        var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        if (exists)
        {
            return (false, $"En användare med e-postadressen \"{email}\" finns redan.");
        }

        var user = new AppUser
        {
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        // Samma grundroll som vid automatisk provisionering via Google-inloggning.
        user.Roles.Add(new UserRole { Role = "User" });

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return (true, null);
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

    public async Task<bool> CreateRoleDefinitionAsync(string name, string? description = null)
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

        _db.Roles.Add(new Role
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task UpdateRoleDescriptionAsync(string name, string? description)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == name);
        if (role is null)
        {
            return;
        }

        role.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        await _db.SaveChangesAsync();
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
