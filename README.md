# Blazor + Google OAuth

Ett minimalt Blazor Web App-projekt (.NET 8, Interactive Server render mode)
med inloggning via Google.

## Innehåll

- `Program.cs` – konfigurerar cookie-autentisering + Google som extern provider,
  samt `/login` och `/logout`-endpoints.
- `Components/Layout/MainLayout.razor` – visar inloggnings-/utloggningslänk.
- `Components/Pages/Home.razor` – startsida, olika innehåll beroende på inloggningsstatus.
- `Components/Pages/Profile.razor` – kräver inloggning (`[Authorize]`), listar alla claims från Google.

## 1. Skapa OAuth-uppgifter i Google Cloud Console

1. Gå till https://console.cloud.google.com/apis/credentials
2. Skapa ett nytt projekt (eller välj ett befintligt).
3. Klicka **Create Credentials → OAuth client ID**.
   - Om det är första gången: konfigurera **OAuth consent screen** först
     (välj "External" om det är för test/personligt bruk, fyll i app-namn och din e-post).
4. Application type: **Web application**.
5. Under **Authorized redirect URIs**, lägg till den URL din app kommer köra på + `/signin-google`, t.ex.:
   - `https://localhost:7160/signin-google` (kolla din port i `Properties/launchSettings.json` efter `dotnet new`, eller se nedan)
6. Klicka **Create**. Du får då ett **Client ID** och **Client secret**.

## 2. Lägg in uppgifterna lokalt (utan att committa hemligheter)

Använd .NET User Secrets istället för att skriva klartext i `appsettings.json`:

```bash
cd BlazorGoogleAuth
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId" "DIN_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "DIN_CLIENT_SECRET"
```

(I produktion: sätt dem som miljövariabler eller i din hemlighetshanterare,
t.ex. `Authentication__Google__ClientId`.)

## 3. Återställ paket och kör

```bash
cd BlazorGoogleAuth
dotnet restore
dotnet run
```

Öppna URL:en som skrivs ut i terminalen (t.ex. `https://localhost:7160`).
Kontrollera att den matchar den redirect URI du angav i Google Cloud Console.

### EkoWeb-sidorna kräver att EkoWebApi också körs

Sidorna under "Ekonomi (EkoWeb)" (`/ekoweb/...`) pratar inte längre direkt med
EkoWeb-databasen - de går via ett separat API-projekt, **EkoWebApi**, som måste
köras samtidigt. Om EkoWebApi inte är igång visar sidorna ett tydligt felmeddelande
istället för att fastna på "Laddar…".

Kör båda projekten samtidigt, t.ex. i två terminalfönster:

```bash
# Terminal 1
dotnet run --project EkoWebApi

# Terminal 2
cd BlazorGoogleAuth
dotnet run
```

I Visual Studio: högerklicka på solutionen → **Set Startup Projects...** →
**Multiple startup projects** → sätt **Action = Start** för både
`BlazorGoogleAuth` och `EkoWebApi`.

Se även [EkoWebApi/appsettings.json](EkoWebApi/appsettings.json) (ConnectionStrings:EkoWeb)
och user-secrets `ApiKey` (måste matcha `EkoWebApi:ApiKey` i BlazorGoogleAuths user-secrets).

## 4. Testa flödet

1. Klicka **Logga in med Google** på startsidan.
2. Godkänn i Googles inloggningsflöde.
3. Du skickas tillbaka till appen, inloggad, och kan besöka `/profile`
   för att se dina claims (namn, e-post, m.m.).

## Hur det fungerar (kort)

- Cookie-autentisering (`AddCookie`) är själva "sign-in scheme":
  när Google-inloggningen lyckas skapas en cookie som håller sessionen.
- `AddGoogle` konfigurerar det externa OAuth-flödet (client id/secret, callback path).
- Eftersom Blazor-komponenter körs interaktivt kan de inte själva skriva
  auth-cookies. Därför triggas inloggning/utloggning via vanliga
  minimal API-endpoints (`/login` och `/logout`) istället för direkt i en komponent.
- `[Authorize]`-attributet på `Profile.razor` gör att sidan kräver inloggning;
  `AuthorizeView` i Razor-markup visar olika innehåll beroende på status.

## Rollbaserad auktorisering (lagras i databas)

Google skickar inga roller – bara identitet (namn, e-post, m.m.). Roller
hanteras därför i en egen databas i din lokala **Microsoft SQL Server**.

**Hur det hänger ihop:**

- **`Data/AppDbContext.cs`** – EF Core-context med tre tabeller:
  - `Users` (Id, Email, DisplayName, CreatedAtUtc)
  - `UserRoles` (Id, AppUserId, Role) – en rad per roll en användare har
  - `Roles` (Id, Name, CreatedAtUtc) – vilka rolltyper som finns tillgängliga att tilldela
- **`Services/UserRoleService.cs`** (`IUserRoleService` / `DbUserRoleService`) –
  all logik för att läsa/skriva användare, roller och rolltyper:
  - `GetOrProvisionRolesAsync(email, displayName)` – slår upp en användare;
    finns den inte skapas den automatiskt med rollen `User`
  - `GetAllUsersAsync()`, `AddRoleAsync(userId, role)`, `RemoveRoleAsync(userId, role)`
  - `GetAllRoleDefinitionsAsync()`, `CreateRoleDefinitionAsync(name)`,
    `DeleteRoleDefinitionAsync(name)` – hanterar själva listan av rolltyper
- **`Services/RoleClaimsTransformation.cs`** – körs efter varje inloggning,
  frågar `IUserRoleService` och lägger till `ClaimTypes.Role`-claims. Det är
  detta som gör att `[Authorize(Roles = "...")]`, `AuthorizeView Roles="..."`
  och `User.IsInRole(...)` fungerar som vanligt.
- **`Components/Pages/AdminUsers.razor`** (`/admin/users`) – ett UI där en
  Admin kan se alla användare som loggat in och lägga till/ta bort roller,
  helt utan att röra kod eller starta om appen.
- **`Components/Pages/AdminRoles.razor`** (`/admin/roles`) – ett UI där en
  Admin kan **skapa nya rolltyper** (t.ex. `Support`, `Ekonomi`) och ta bort
  oanvända rolltyper. En rolltyp som redan är tilldelad någon användare kan
  inte tas bort förrän den plockats bort från alla användare först, för att
  undvika att användare tappar roller "spökaktigt".

De tre grundrollerna `User`, `Editor` och `Admin` skapas automatiskt i
`Roles`-tabellen första gången appen körs (se seedningen i `Program.cs`).
Du kan lägga till hur många fler rolltyper du vill via `/admin/roles`.

**Connection string till din lokala SQL Server:**

I `appsettings.json`:

```json
"ConnectionStrings": {
  "Default": "Server=localhost;Database=BlazorGoogleAuth;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Anpassa `Server=` beroende på hur din lokala instans är konfigurerad:

| Situation | Server-värde |
|---|---|
| Standardinstans | `localhost` eller `.` |
| Named instance (t.ex. SQL Server Express) | `localhost\SQLEXPRESS` |
| Specifik port | `localhost,1433` |
| SQL-inloggning istället för Windows-autentisering | Byt `Trusted_Connection=True` mot `User Id=sa;Password=DittLösenord;` |

`TrustServerCertificate=True` behövs oftast lokalt för att slippa SSL-certifikatfel
mot en dev-instans utan giltigt certifikat. Databasen `BlazorGoogleAuth` behöver
**inte** skapas manuellt – det görs automatiskt (se nedan), men SQL Server-instansen
måste redan köra och vara nåbar.

Om du hellre vill hålla uppgifterna utanför appsettings.json (rekommenderas om
du använder SQL-inloggning med lösenord), sätt den via User Secrets istället:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Database=BlazorGoogleAuth;User Id=sa;Password=DittLösenord;TrustServerCertificate=True;"
```

**Bootstrap av första admin-kontot:**

Eftersom ingen är Admin i en tom databas första gången du kör appen, finns
en engångs-genväg i `appsettings.json`:

```json
"Authorization": {
  "AdminEmails": [ "din.epost@gmail.com" ]
}
```

Första gången en e-post i den listan loggar in får den automatiskt rollen
`Admin` (utöver grundrollen `User`), och läggs in i databasen. Därefter är
det databasen (via `/admin/users`) som gäller – du kan lämna listan kvar
(den påverkar bara *nya* användare som inte redan finns i databasen) eller
tömma den när du har minst en admin.

**Databasen och tabellerna:**

- Skapas automatiskt vid uppstart via `db.Database.EnsureCreated()` i
  `Program.cs` – appen skapar både databasen `BlazorGoogleAuth` (om den inte
  finns) och tabellerna `Users`/`UserRoles`/`Roles` första gången den körs.
  Inga separata migrationskommandon behövs för att komma igång.
- ⚠️ **Viktigt om du redan körde en tidigare version av projektet** (innan
  `Roles`-tabellen fanns): `EnsureCreated()` ändrar **inte** schemat i en
  databas som redan finns – den bara skapar den om den saknas helt. Om du
  redan har en `BlazorGoogleAuth`-databas från innan behöver du antingen:
  1. Ta bort den (t.ex. `DROP DATABASE BlazorGoogleAuth;` i SSMS) så skapas
     den om med rätt schema nästa gång du kör appen, eller
  2. Skapa `Roles`-tabellen manuellt, eller
  3. Byta till riktiga migrationer (se nedan) och köra `dotnet ef database update`.
- Vill du använda riktiga EF Core-migrationer istället (rekommenderas i
  produktion, eller så fort du vill ändra schemat utan att tappa data),
  installera verktyget och byt ut `EnsureCreated()` mot `Migrate()`:

  ```bash
  dotnet tool install --global dotnet-ef
  cd BlazorGoogleAuth
  dotnet ef migrations add InitialCreate
  dotnet ef database update
  ```

  Ändra sedan i `Program.cs`:
  ```csharp
  db.Database.Migrate(); // istället för db.Database.EnsureCreated();
  ```

**Skydda en sida med en specifik roll:**

```razor
@page "/admin"
@attribute [Authorize(Roles = "Admin")]
```

**Skydda en sida med flera roller (via policy):**

```razor
@page "/editor"
@attribute [Authorize(Policy = "RequireEditorOrAdmin")]
```

Policyn definieras i `Program.cs`:

```csharp
options.AddPolicy("RequireEditorOrAdmin", policy => policy.RequireRole("Editor", "Admin"));
```

**Visa/dölja innehåll i markup baserat på roll:**

```razor
<AuthorizeView Roles="Admin">
    <Authorized>Bara admins ser detta</Authorized>
    <NotAuthorized>Alla andra ser detta</NotAuthorized>
</AuthorizeView>
```

**Vad händer om en inloggad användare utan rätt roll försöker nå en skyddad sida?**
De skickas till `/access-denied` (konfigurerat i `Program.cs` via
`options.AccessDeniedPath`). Icke-inloggade skickas istället till `/login`
(`options.LoginPath`), precis som innan.

**Exempelsidor i projektet:**

| Sida | Skydd |
|---|---|
| `/` | Öppen för alla, olika innehåll beroende på roll |
| `/profile` | Kräver inloggning, visar användarens roller + claims |
| `/editor` | Kräver rollen `Editor` eller `Admin` |
| `/admin` | Kräver rollen `Admin` |
| `/admin/users` | Kräver rollen `Admin` – lägg till/ta bort roller för alla användare |
| `/admin/roles` | Kräver rollen `Admin` – skapa/ta bort rolltyper |

## Vanliga problem

- **redirect_uri_mismatch**: redirect-URI:n i Google Cloud Console matchar inte
  exakt (inklusive port och `https`/`http`) den som appen faktiskt använder.
- **400: invalid_client**: fel Client ID/Secret, eller inte sparat med User Secrets korrekt.
- **EkoWeb-sidorna fastnar på "Laddar…" eller visar "Kunde inte nå EkoWebApi"**:
  EkoWebApi-projektet körs inte. Se avsnittet om EkoWebApi under punkt 3 ovan.
- Om du kör bakom en proxy/reverse proxy (t.ex. i produktion), lägg till
  `app.UseForwardedHeaders()` så att callback-URL:en byggs korrekt.
