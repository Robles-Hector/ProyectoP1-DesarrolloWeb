using SakilaRentalApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Configuración de servicios MVC
builder.Services.AddControllersWithViews();

// Configuración de Sesiones
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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Middleware de Sesión indispensable para la autenticación y control de roles
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
