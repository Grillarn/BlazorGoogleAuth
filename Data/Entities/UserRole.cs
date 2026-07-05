namespace BlazorGoogleAuth.Data.Entities;

/// <summary>
/// En roll tilldelad en specifik användare, t.ex. ("martin@gmail.com", "Admin").
/// En användare kan ha flera rader/roller.
/// </summary>
public class UserRole
{
    public int Id { get; set; }

    public int AppUserId { get; set; }

    public AppUser AppUser { get; set; } = null!;

    public required string Role { get; set; }
}
