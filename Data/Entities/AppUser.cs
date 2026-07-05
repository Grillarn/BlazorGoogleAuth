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

    /// <summary>
    /// Googles egen, permanenta konto-ID ("sub"-claim). Ändras aldrig,
    /// till skillnad från e-post - praktiskt om en användare byter e-post.
    /// </summary>
    public string? GoogleId { get; set; }

    public string? GivenName { get; set; }

    public string? Surname { get; set; }

    public string? PictureUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<UserRole> Roles { get; set; } = [];
}
