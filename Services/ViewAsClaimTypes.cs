namespace BlazorGoogleAuth.Services;

/// <summary>
/// Claim som markerar att personen egentligen är Admin, även när de just nu
/// "kör som" en annan (lägre) roll - annars skulle de inte kunna växla tillbaka.
/// </summary>
public static class ViewAsClaimTypes
{
    public const string RealAdmin = "eko:real-admin";
}
