using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using ApartmanYonetim.Infrastructure.Identity;
using ApartmanYonetim.Infrastructure.Persistence;
using ApartmanYonetim.Infrastructure.Services;
using ApartmanYonetim.Infrastructure.Tenant;
using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Web.Components;
using ApartmanYonetim.Web.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/giris";
    options.AccessDeniedPath = "/";
});

// On Azure App Service, store SQLite files under /home/data (persists across deployments).
// Locally, fall back to appsettings.json values.
var azureHome = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") is not null
    ? (Environment.GetEnvironmentVariable("HOME") ?? "/home")
    : null;

var connectionString = azureHome is not null
    ? $"DataSource={azureHome}/data/apartman-main.db;Cache=Shared"
    : builder.Configuration.GetConnectionString("DefaultConnection")
      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<MainDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentityCore<AppUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<MainDbContext>()
    .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Tenant infrastructure
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Firm-level DB: created per-scope, connection string resolved from ITenantContext
var firmDbDir = azureHome is not null
    ? $"{azureHome}/data/FirmDatabases"
    : builder.Configuration["FirmDbDirectory"] ?? "FirmDatabases";
Directory.CreateDirectory(firmDbDir);
builder.Services.AddSingleton(new FirmDbContextFactory(firmDbDir));
builder.Services.AddScoped<FirmDbContext>(sp =>
{
    var tenant = sp.GetRequiredService<ITenantContext>();
    var factory = sp.GetRequiredService<FirmDbContextFactory>();
    if (!tenant.HasFirm)
    {
        // SuperAdmin has no firm: use an open in-memory connection so EF Core never closes it
        // between commands (closing :memory: destroys the database and drops the schema).
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var opts = new DbContextOptionsBuilder<FirmDbContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new FirmDbContext(opts);
        ctx.Database.EnsureCreated();
        return ctx;
    }
    return factory.CreateBySlug(tenant.FirmSlug!);
});

var siteDbDir = azureHome is not null
    ? $"{azureHome}/data/SiteDatabases"
    : builder.Configuration["SiteDbDirectory"] ?? "SiteDatabases";
Directory.CreateDirectory(siteDbDir);
builder.Services.AddSingleton(new SiteDbContextFactory(siteDbDir));

builder.Services.AddScoped<IFirmRegistrationService, FirmRegistrationService>();
builder.Services.AddScoped<IManagementCompanyService, ManagementCompanyService>();
builder.Services.AddScoped<ISiteManagementService, SiteManagementService>();
builder.Services.AddScoped<ISiteResidentService, SiteResidentService>();
builder.Services.AddScoped<ISiteFeeService, SiteFeeService>();
builder.Services.AddScoped<ISiteAnnouncementService, SiteAnnouncementService>();
builder.Services.AddScoped<ISiteMaintenanceService, SiteMaintenanceService>();
builder.Services.AddScoped<ISiteMeetingService, SiteMeetingService>();
builder.Services.AddScoped<ISiteAccountingService, SiteAccountingService>();
builder.Services.AddScoped<ISiteReportService, SiteReportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

// Login endpoint
app.MapPost("/api/auth/login", async (
    HttpContext ctx,
    ITenantContext tenantCtx,
    SignInManager<AppUser> signInMgr,
    UserManager<AppUser> userMgr) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString() is { Length: > 0 } r ? r : "/";

    var user = await userMgr.FindByEmailAsync(email);
    if (user is null) { ctx.Response.Redirect($"/giris?hata=1&returnUrl={Uri.EscapeDataString(returnUrl)}"); return; }

    // Set tenant context before signing in so FirmDbContext resolves correctly if needed
    if (user.FirmSlug is not null)
        tenantCtx.SetFirmSlug(user.FirmSlug);

    var result = await signInMgr.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
    ctx.Response.Redirect(result.Succeeded ? returnUrl : $"/giris?hata=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
});

// Logout endpoints
app.MapPost("/api/auth/logout", async (HttpContext ctx, SignInManager<AppUser> signInMgr) =>
{
    await signInMgr.SignOutAsync();
    ctx.Response.Redirect("/giris");
});

app.MapGet("/cikis", async (HttpContext ctx, SignInManager<AppUser> signInMgr) =>
{
    await signInMgr.SignOutAsync();
    ctx.Response.Redirect("/giris");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<MainDbContext>();
    var dbDataSource = connectionString.Split(';')
        .Select(p => p.Trim())
        .FirstOrDefault(p => p.StartsWith("DataSource=", StringComparison.OrdinalIgnoreCase))
        ?.Substring("DataSource=".Length) ?? "data/apartman-main.db";
    var dbDir = Path.GetDirectoryName(dbDataSource);
    if (!string.IsNullOrEmpty(dbDir)) Directory.CreateDirectory(dbDir);
    await db.Database.MigrateAsync();
    var userMgr = services.GetRequiredService<UserManager<AppUser>>();
    var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
    var firmFactory = services.GetRequiredService<FirmDbContextFactory>();
    var siteFactory = services.GetRequiredService<SiteDbContextFactory>();
    await DbSeeder.SeedAsync(db, userMgr, roleMgr, firmFactory, siteFactory);
}

app.Run();
