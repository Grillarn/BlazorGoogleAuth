using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using BlazorGoogleAuth.Components;
using BlazorGoogleAuth.Data;
using BlazorGoogleAuth.Data.Entities;
using BlazorGoogleAuth.Services;
using BlazorGoogleAuth.Services.EkoWeb;

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

// EkoWeb (ekonomi, konton m.m.) hanteras inte längre via direkt databasåtkomst
// härifrån - det är ett eget API (se ../EkoWebApi) som äger den databasen.
// Vi pratar bara HTTP med det, skyddat med en delad nyckel i X-Api-Key.
builder.Services.AddHttpClient<IEkoWebService, EkoWebApiClient>(client =>
{
    var baseUrl = builder.Configuration["EkoWebApi:BaseUrl"]
        ?? throw new InvalidOperationException("EkoWebApi:BaseUrl saknas i konfigurationen.");
    var apiKey = builder.Configuration["EkoWebApi:ApiKey"]
        ?? throw new InvalidOperationException("EkoWebApi:ApiKey saknas i konfigurationen.");

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
});

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

// --- "Kör som en annan roll" ---
// Låter en riktig Admin tillfälligt se/uppleva appen som om de bara hade en
// annan roll (t.ex. User eller Editor), utan att ändra något i databasen.
// Görs genom att byta ut rollclaimen i inloggningscookien - eftersom allt annat
// i appen ([Authorize(Roles=...)], AuthorizeView, EkoWebs admin-koll) redan
// bygger på den claimen fungerar det konsekvent överallt utan specialkod.
// En dold "eko:real-admin"-claim sparas med så man alltid kan växla tillbaka.

app.MapPost("/admin/view-as", async (HttpContext httpContext, [Microsoft.AspNetCore.Mvc.FromForm] string role) =>
{
    var user = httpContext.User;
    var isRealAdmin = user.IsInRole("Admin") || user.HasClaim(ViewAsClaimTypes.RealAdmin, "true");
    if (!isRealAdmin)
    {
        return Results.Forbid();
    }

    var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
    foreach (var claim in user.Claims.Where(c => c.Type != ClaimTypes.Role && c.Type != ViewAsClaimTypes.RealAdmin))
    {
        identity.AddClaim(claim);
    }
    identity.AddClaim(new Claim(ClaimTypes.Role, role));
    identity.AddClaim(new Claim(ViewAsClaimTypes.RealAdmin, "true"));

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Redirect("/");
}).DisableAntiforgery();

app.MapPost("/admin/view-as/reset", async (HttpContext httpContext, IUserRoleService userRoleService) =>
{
    var user = httpContext.User;
    if (!user.HasClaim(ViewAsClaimTypes.RealAdmin, "true") && !user.IsInRole("Admin"))
    {
        return Results.Forbid();
    }

    var email = user.FindFirst(ClaimTypes.Email)?.Value;
    if (string.IsNullOrWhiteSpace(email))
    {
        return Results.BadRequest();
    }

    // Hämta de riktiga rollerna från databasen igen (samma som vid inloggning),
    // istället för att bara lägga tillbaka "Admin" - ifall rollerna hunnit
    // ändras under tiden.
    var displayName = user.FindFirst(ClaimTypes.Name)?.Value;
    var googleId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var givenName = user.FindFirst(ClaimTypes.GivenName)?.Value;
    var surname = user.FindFirst(ClaimTypes.Surname)?.Value;
    var pictureUrl = user.FindFirst("picture")?.Value;
    var profile = new GoogleProfile(email, displayName, googleId, givenName, surname, pictureUrl);
    var realRoles = await userRoleService.GetOrProvisionRolesAsync(profile);

    var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
    foreach (var claim in user.Claims.Where(c => c.Type != ClaimTypes.Role && c.Type != ViewAsClaimTypes.RealAdmin))
    {
        identity.AddClaim(claim);
    }
    foreach (var role in realRoles)
    {
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
    }

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Redirect("/");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
