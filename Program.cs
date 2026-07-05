using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using BlazorGoogleAuth.Components;
using BlazorGoogleAuth.Data;
using BlazorGoogleAuth.Data.Entities;
using BlazorGoogleAuth.Services;

var builder = WebApplication.CreateBuilder(args);

// SQL Server-databas för användare och roller.
// Anpassa connection string i appsettings.json (ConnectionStrings:Default)
// om din lokala SQL Server-instans heter något annat än standard.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default saknas i konfigurationen.")));

// Razor Components (Blazor Server / Interactive Server render mode)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Autentisering: cookie som "sign in scheme" + Google som extern provider
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
    })
    .AddGoogle(options =>
    {
        // Läses från appsettings.json / User Secrets / miljövariabler
        // (se README.md för hur du skapar dessa i Google Cloud Console)
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Authentication:Google:ClientId saknas i konfigurationen.");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Authentication:Google:ClientSecret saknas i konfigurationen.");

        // Måste matcha en "Authorized redirect URI" i Google Cloud Console,
        // t.ex. https://localhost:7160/signin-google
        options.CallbackPath = "/signin-google";

        // Valfritt: hämta fler profiluppgifter
        options.Scope.Add("profile");
        options.SaveTokens = true;
    });

// Rollhantering: mappar Google-inloggad e-post -> roller via databasen (se Services/ och Data/).
builder.Services.AddScoped<IUserRoleService, DbUserRoleService>();
builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();

builder.Services.AddAuthorization(options =>
{
    // Namngivna policys ovanpå de vanliga rollerna - praktiskt när en sida
    // ska tillåta flera roller, t.ex. både Admin och Editor.
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireEditorOrAdmin", policy => policy.RequireRole("Editor", "Admin"));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Skapar databasen (och tabellerna) om den inte redan finns.
// För ett riktigt produktionsflöde: byt ut mot EF Core-migrationer
// (dotnet ef migrations add ... + db.Database.Migrate()) - se README.md.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Seeda grundläggande rolltyper första gången appen körs, så att
    // /admin/users har något att välja på innan en admin hunnit skapa fler.
    foreach (var roleName in new[] { "User", "Editor", "Admin" })
    {
        if (!db.Roles.Any(r => r.Name == roleName))
        {
            db.Roles.Add(new Role { Name = roleName, CreatedAtUtc = DateTime.UtcNow });
        }
    }
    db.SaveChanges();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// --- Inloggnings-/utloggningsendpoints ---
// Blazor-komponenter kan inte själva sätta auth-cookies under interaktiv rendering,
// därför hanteras Challenge/SignOut via vanliga minimal API-endpoints.

app.MapGet("/login", (string? returnUrl) =>
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl
    };
    return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
});

app.MapPost("/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
