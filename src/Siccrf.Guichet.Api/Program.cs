using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Siccrf.Guichet.Api.Infrastructure;
using Siccrf.Guichet.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Authentification JWT (démonstrateur)
builder.Services.Configure<OptionsJwt>(builder.Configuration.GetSection(OptionsJwt.Section));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(OptionsJwt.Section).Get<OptionsJwt>() ?? new OptionsJwt();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emetteur,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Cle)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// Base de données SQLite
builder.Services.AddDbContext<GuichetDbContext>((sp, options) =>
{
    // Un chemin relatif est résolu depuis le dossier du projet (et non depuis le dossier de
    // travail du processus, qui varie : IIS Express, Kestrel, dotnet run…).
    var chaine = new SqliteConnectionStringBuilder(
        sp.GetRequiredService<IConfiguration>().GetConnectionString("Guichet"));

    if (!Path.IsPathRooted(chaine.DataSource) && chaine.DataSource != ":memory:")
    {
        chaine.DataSource = Path.Combine(
            sp.GetRequiredService<IWebHostEnvironment>().ContentRootPath,
            chaine.DataSource);
    }

    options.UseSqlite(chaine.ToString());
});

// Assistant IA : simulé par défaut, réel (OpenAI/Azure OpenAI) si configuré
builder.Services.Configure<OptionsAssistantOpenAi>(builder.Configuration.GetSection(OptionsAssistantOpenAi.Section));
builder.Services.AddHttpClient<AssistantOpenAi>();
builder.Services.AddScoped<IAssistantService>(sp =>
{
    var options = sp.GetRequiredService<IOptions<OptionsAssistantOpenAi>>().Value;
    var config = sp.GetRequiredService<IConfiguration>();
    var mode = config["Assistant:Mode"] ?? "simule";

    return mode.Equals("reel", StringComparison.OrdinalIgnoreCase)
        ? sp.GetRequiredService<AssistantOpenAi>()
        : sp.GetRequiredService<AssistantSimule>();
});
builder.Services.AddScoped<AssistantSimule>();
builder.Services.AddScoped<INotificationService, NotificationLogService>();
builder.Services.AddScoped<DemandeService>();
builder.Services.AddScoped<ExportFicheService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// --- Pipeline HTTP ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.UseAuthentication();
app.UseAuthorization();

// Frontend vanilla JS (incrément 5)
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/sante", () => Results.Ok(new { statut = "ok", horodatage = DateTime.UtcNow }));

// --- Initialisation de la base (seed de données fictives) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GuichetDbContext>();
    SeedDonnees.Initialiser(db);
}

app.Run();

/// <summary>Point d'entrée pour les tests d'intégration.</summary>
public partial class Program;