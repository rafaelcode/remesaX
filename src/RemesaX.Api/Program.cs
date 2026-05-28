using RemesaX.Core.Interfaces;
using RemesaX.Infrastructure.Stellar;
using RemesaX.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// Validación temprana de configuración Stellar
// =============================================================================
var stellarSection = builder.Configuration.GetSection("Stellar");
var stellarIssuerPublic = stellarSection["IssuerPublicKey"] ?? "";
var stellarIssuerSecret = stellarSection["IssuerSecretKey"] ?? "";

Console.WriteLine("════════════════════════════════════════════════════════════");
Console.WriteLine("🔧 Cargando configuración Stellar:");
Console.WriteLine($"   Network:         {stellarSection["Network"]}");
Console.WriteLine($"   HorizonUrl:      {stellarSection["HorizonUrl"]}");
Console.WriteLine($"   AssetCode:       {stellarSection["AssetCode"]}");
Console.WriteLine($"   IssuerPublicKey: '{stellarIssuerPublic}' (length: {stellarIssuerPublic.Length})");
Console.WriteLine($"   IssuerSecretKey: '{(stellarIssuerSecret.Length > 8 ? stellarIssuerSecret[..4] + "..." + stellarIssuerSecret[^4..] : stellarIssuerSecret)}' (length: {stellarIssuerSecret.Length})");

if (stellarIssuerPublic.Length != 56 || !stellarIssuerPublic.StartsWith("G"))
{
    Console.WriteLine("⚠️  ALERTA: IssuerPublicKey NO es una public key Stellar válida.");
}
else
{
    Console.WriteLine("✅ IssuerPublicKey parece válida.");
}
Console.WriteLine("════════════════════════════════════════════════════════════\n");

// Logging estructurado con Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "RemesaX API",
        Version = "v1",
        Description = "MVP de remesas LATAM sobre Stellar blockchain"
    });
});

// Configuración de Stellar
builder.Services.Configure<StellarSettings>(builder.Configuration.GetSection("Stellar"));
builder.Services.AddSingleton<IStellarService, StellarService>();
builder.Services.AddScoped<IRemittanceService, RemittanceService>();

// Base de datos
builder.Services.AddDbContext<RemesaXDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=remesax.db"));

builder.Services.AddHealthChecks();

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Migración automática
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RemesaXDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🌐 Frontend estático: servir archivos desde wwwroot/
app.UseDefaultFiles();   // index.html como default cuando se accede a /
app.UseStaticFiles();    // habilita servir /css/*, /js/*, etc.

app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();