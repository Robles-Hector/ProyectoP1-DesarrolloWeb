using Microsoft.AspNetCore.Localization;
using SakilaRentalApp.Data;
using SakilaRentalApp.Utils;
using System.Globalization;

// Determinar las rutas de contenido y archivos estáticos (wwwroot) automáticamente,
// permitiendo ejecutar la app tanto desde Visual Studio, dotnet run, o directamente desde bin/Debug/net10.0
string baseDir = AppContext.BaseDirectory;
string? projectDir = Directory.GetParent(baseDir)?.Parent?.Parent?.FullName;
string contentRoot = (projectDir != null && Directory.Exists(Path.Combine(projectDir, "wwwroot")))
    ? projectDir
    : baseDir;

string webRoot = Directory.Exists(Path.Combine(contentRoot, "wwwroot"))
    ? Path.Combine(contentRoot, "wwwroot")
    : (Directory.Exists(Path.Combine(baseDir, "wwwroot")) ? Path.Combine(baseDir, "wwwroot") : "wwwroot");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot,
    WebRootPath = webRoot
});

// Configuración de puerto seguro (5217) para evitar conflictos con el puerto 5000 en el equipo
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5217");
}

// Configuración de servicios MVC con soporte universal para decimales
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
});

// Configuración de Sesiones (Session)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".SakilaRental.Session";
});

// Inyección de dependencias para Acceso a Datos (ADO.NET)
builder.Services.AddSingleton<DatabaseHelper>();
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<FilmRepository>();
builder.Services.AddScoped<InventoryRepository>();
builder.Services.AddScoped<RentalRepository>();
builder.Services.AddScoped<ReporteRepository>();

var app = builder.Build();

// Configuración de cultura para formateo consistente de fechas y moneda
var defaultCulture = new CultureInfo("es-EC");
defaultCulture.NumberFormat.NumberDecimalSeparator = ".";
defaultCulture.NumberFormat.CurrencyDecimalSeparator = ".";

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};
app.UseRequestLocalization(localizationOptions);

// Inicialización automática de la base de datos (Retos 1 y 2 + Seed de usuarios iniciales)
using (var scope = app.Services.CreateScope())
{
    var dbHelper = scope.ServiceProvider.GetRequiredService<DatabaseHelper>();
    await DbInitializer.InitializeAsync(dbHelper, app.Logger);
}

// Configuración del pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

// Middleware de Sesión indispensable para la autenticación y control de roles
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
