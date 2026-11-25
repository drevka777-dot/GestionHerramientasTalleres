
using GestionHerramientasTalleres.Components;
using GestionHerramientasTalleres.Data;
using GestionHerramientasTalleres.Models;
using GestionHerramientasTalleres.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// BASE DE DATOS DEL INVENTARIO Y SALAS
// ========================================

// Conservamos la base de datos existente.
// Contiene las tablas Herramientas y Salas.

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=gestionherramientas.db"));

// CORREGIDO:
// InventarioService utiliza AuthenticationStateProvider.
// Por eso debe ser Scoped y no Singleton.

builder.Services.AddScoped<InventarioService>();

// Servicio para administrar talleres y laboratorios.

builder.Services.AddScoped<SalaService>();
builder.Services.AddScoped<PrestamoService>();

// ========================================
// BASE DE DATOS DE USUARIOS
// ========================================

// Base de datos independiente para Identity.
// Conserva los usuarios y sus roles existentes.

builder.Services.AddDbContext<UsuariosDbContext>(options =>
    options.UseSqlite("Data Source=usuarios.db"));

// ========================================
// AUTENTICACIÓN Y ROLES
// ========================================

builder.Services
    .AddIdentity<UsuarioSistema, IdentityRole>(options =>
    {
        // Requisitos de contraseña.

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // Correos electrónicos únicos.

        options.User.RequireUniqueEmail = true;

        // Protección contra intentos fallidos.

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(5);

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<UsuariosDbContext>()
    .AddDefaultTokenProviders();

// ========================================
// CONFIGURACIÓN DE COOKIES
// ========================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";

    options.AccessDeniedPath = "/acceso-denegado";

    options.ExpireTimeSpan = TimeSpan.FromHours(8);

    options.SlidingExpiration = true;

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;
});

// ========================================
// AUTORIZACIÓN Y BLAZOR
// ========================================

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ========================================
// INICIALIZACIÓN DEL INVENTARIO
// ========================================

// Conservamos la información existente.
// Las herramientas iniciales se agregan
// únicamente si la tabla está vacía.

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<AppDbContext>>();

    using var db = dbFactory.CreateDbContext();

    // Las bases anteriores tienen las dos migraciones de inventario.
    // Aplicar únicamente las pendientes; una base nueva se crea completa.
    if (db.Database.GetAppliedMigrations().Any())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();

    if (!db.Herramientas.Any())
    {
        db.Herramientas.AddRange(
            new Herramienta
            {
                Codigo = "HER-001",
                Nombre = "Taladro eléctrico",
                Disponible = true,
                SalaId = null
            },
            new Herramienta
            {
                Codigo = "HER-002",
                Nombre = "Multímetro digital",
                Disponible = false,
                SalaId = null
            },
            new Herramienta
            {
                Codigo = "HER-003",
                Nombre = "Juego de destornilladores",
                Disponible = true,
                SalaId = null
            }
        );

        db.SaveChanges();
    }
}

// ========================================
// INICIALIZACIÓN DE ROLES
// ========================================

// Garantizamos que los roles existan.
// No modificamos los permisos de las
// cuentas que ya están registradas.

using (var scope = app.Services.CreateScope())
{
    var usuariosDb = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
    if (usuariosDb.Database.GetAppliedMigrations().Any())
        usuariosDb.Database.Migrate();
    else
        usuariosDb.Database.EnsureCreated();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles =
    {
        "Usuario",
        "Administrador"
    };

    foreach (var nombreRol in roles)
    {
        if (!await roleManager.RoleExistsAsync(nombreRol))
        {
            var resultado = await roleManager.CreateAsync(
                new IdentityRole(nombreRol));

            if (!resultado.Succeeded)
            {
                var errores = string.Join("; ",
                    resultado.Errors.Select(
                        e => e.Description));

                throw new InvalidOperationException(
                    $"No se pudo crear el rol {nombreRol}: {errores}");
            }
        }
    }
}

// ========================================
// CONFIGURACIÓN DE ERRORES
// ========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

// ========================================
// MIDDLEWARE DE SEGURIDAD
// ========================================

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseAntiforgery();

// ========================================
// ARCHIVOS ESTÁTICOS
// ========================================

app.MapStaticAssets();

// ========================================
// CERRAR SESIÓN
// ========================================

// El endpoint POST permite cerrar sesión
// mediante ASP.NET Core Identity.

app.MapPost("/logout", async (
    SignInManager<UsuarioSistema> signInManager) =>
{
    await signInManager.SignOutAsync();

    return Results.LocalRedirect("/login");
})
.RequireAuthorization();

// ========================================
// COMPONENTES BLAZOR
// ========================================

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ========================================
// INICIAR APLICACIÓN
// ========================================

app.Run();
