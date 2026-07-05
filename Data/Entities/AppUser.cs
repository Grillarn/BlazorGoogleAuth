namespace BlazorGoogleAuth.Data.Entities;

/// <summary>
/// En användare som loggat in minst en gång via Google.
/// Skapas automatiskt (provisioneras) första gången personen loggar in.
/// </summary>
public class AppUser
{
    public int Id { get; set; }

    public required string Email { get; set; }

    public string? DisplayName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<UserRole> Roles { get; set; } = [];
}
