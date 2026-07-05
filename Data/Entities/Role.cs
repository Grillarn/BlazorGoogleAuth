namespace BlazorGoogleAuth.Data.Entities;

/// <summary>
/// En rolltyp som finns tillgänglig i systemet, t.ex. "Admin", "Editor", "Support".
/// Adminanvändare kan skapa (och ta bort oanvända) rolltyper via /admin/roles,
/// utan att ändra kod eller starta om appen.
/// </summary>
public class Role
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
