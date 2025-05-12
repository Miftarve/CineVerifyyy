using CineVerify.Data;
using CineVerify.Models;
using CineVerify.Repositories;
using CineVerify.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configurazione autenticazione Google
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.CallbackPath = "/signin-google"; // Questo è il percorso predefinito
    });

// Configura le Razor Pages per permettere accesso pubblico alle pagine principali
builder.Services.AddRazorPages(options =>
{
    // Specifica quali pagine o cartelle richiedono autenticazione
    options.Conventions.AuthorizeFolder("/Account/Manage");
    options.Conventions.AuthorizeFolder("/Admin");

    // Rendi esplicitamente pubbliche le pagine che dovrebbero essere accessibili a tutti
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToFolder("/Movies");
    options.Conventions.AllowAnonymousToPage("/Error");
    options.Conventions.AllowAnonymousToPage("/Privacy");
    options.Conventions.AllowAnonymousToPage("/About");
    options.Conventions.AllowAnonymousToPage("/Contact");
});

// Configurazione per i controller Web API
builder.Services.AddControllers()
    .ConfigureApplicationPartManager(manager =>
    {
        // Questo fa sì che ASP.NET Core cerchi i controller in tutto l'assembly
        // inclusi quelli nella cartella Modules e Controllers
        var assembly = typeof(Program).Assembly;
        manager.ApplicationParts.Add(new AssemblyPart(assembly));
    });

// Supporto per API Explorer per Swagger o altre documentazioni API
builder.Services.AddEndpointsApiExplorer();

// Configura il cookie di autenticazione per evitare reindirizzamenti automatici
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    // Non reindirizzare automaticamente alla pagina di login per richieste AJAX o API
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy("ModeratorPolicy", policy =>
        policy.RequireRole("Moderator", "Admin"));

    // Aggiungiamo una policy per le azioni sensibili
    options.AddPolicy("RegisteredUserPolicy", policy =>
        policy.RequireAuthenticatedUser());
});

// Registrazione base di HttpClient
builder.Services.AddHttpClient();

// Servizi custom
builder.Services.AddScoped<GeminiService>();
builder.Services.AddHttpClient<GeminiService>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Servizio API per film
builder.Services.AddScoped<MovieApiService>();

builder.Services.AddHttpClient<MovieApiService>(client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<AdvancedMovieInfoService>();
builder.Services.AddHttpClient<AdvancedMovieInfoService>();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Mappa i controller API
app.MapControllers();

// Mantieni questa mappatura per le Razor Pages
app.MapRazorPages();

app.Run();

// Metodi di supporto per l'inizializzazione
async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
{
    string[] roleNames = { "Admin", "Moderator", "User" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

async Task CreateAdminUserAsync(UserManager<ApplicationUser> userManager)
{
    var adminEmail = "admin@cineverify.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            Nome = "Admin",
            Cognome = "CineVerify",
            DataRegistrazione = DateTime.UtcNow,
            EmailConfirmed = true,
            ProfilePictureUrl = "/images/default-avatar.png"
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}