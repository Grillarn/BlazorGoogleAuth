using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using EkoWebApi.Data;
using EkoWebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EkoWeb API",
        Version = "v1",
        Description = "API för EkoWeb-databasen (ekonomi, konton, kategorier m.m.). Anropas av BlazorGoogleAuth server-till-server."
    });

    // Låter Swagger UI visa en "Authorize"-knapp där man klistrar in
    // X-Api-Key en gång, så den skickas med automatiskt i "Try it out".
    const string apiKeyScheme = "ApiKey";
    options.AddSecurityDefinition(apiKeyScheme, new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Delad hemlig nyckel, samma värde som ApiKey i konfigurationen."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = apiKeyScheme }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<EkoWebDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("EkoWeb")
            ?? throw new InvalidOperationException("ConnectionStrings:EkoWeb saknas i konfigurationen.")));

builder.Services.AddScoped<IEkoWebService, EkoWebService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enkelt delad-hemlighet-skydd: den här API:et exponerar riktig ekonomidata
// och anropas bara server-till-server från BlazorGoogleAuth, så en delad
// nyckel i en header räcker (ingen inloggad slutanvändare pratar direkt med API:et).
var apiKey = builder.Configuration["ApiKey"]
    ?? throw new InvalidOperationException("ApiKey saknas i konfigurationen (sätt via user-secrets).");

app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("X-Api-Key", out var providedKey) ||
        !string.Equals(providedKey, apiKey, StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Ogiltig eller saknad X-Api-Key.");
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

app.Run();
