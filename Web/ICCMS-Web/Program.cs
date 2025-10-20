using DinkToPdf;
using DinkToPdf.Contracts;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

var builder = WebApplication.CreateBuilder(args);

// ===================================================
// 🔧 SERVICE REGISTRATION
// ===================================================

// HTTP + API
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // 30 second timeout
    client.DefaultRequestHeaders.Add("User-Agent", "ICCMS-Web/1.0");
});
builder.Services.AddScoped<IApiClient, ApiClient>();

// Access HttpContext and TempData inside services (needed for Auth redirect handling)
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITempDataDictionaryFactory, TempDataDictionaryFactory>();

// Login attempt tracking
builder.Services.AddSingleton<ILoginAttemptService, LoginAttemptService>();

// In-memory project index service (per-user)
builder.Services.AddSingleton<IProjectIndexService, ProjectIndexService>();

//Register Dink To PDF for pdf
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

// MVC / Razor Views
builder.Services.AddControllersWithViews();

// ===================================================
// 💾 SESSION MANAGEMENT
// ===================================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===================================================
// 🔒 AUTHENTICATION CONFIGURATION
// ===================================================
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Cookies";
        options.DefaultSignInScheme = "Cookies";
        options.DefaultChallengeScheme = "Cookies";
    })
    .AddCookie(
        "Cookies",
        options =>
        {
            // NOTE: redirect paths must align with actual controllers
            options.LoginPath = "/Auth/Login";
            options.LogoutPath = "/Auth/Logout";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;

            // Optional: friendly redirect if unauthorized
            options.AccessDeniedPath = "/Auth/AccessDenied";
        }
    );

// ===================================================
// 🧩 AUTHORIZATION POLICIES
// ===================================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy(
        "ProjectManagerOnly",
        policy => policy.RequireRole("Project Manager", "Tester")
    );
    options.AddPolicy("ContractorOnly", policy => policy.RequireRole("Contractor"));
    options.AddPolicy("ClientOnly", policy => policy.RequireRole("Client"));
});

// ===================================================
// 🚀 BUILD APP
// ===================================================
var app = builder.Build();

// ===================================================
// 🌍 MIDDLEWARE PIPELINE
// ===================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Order matters: Session before Auth
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ===================================================
// 🏠 ROUTING
// ===================================================
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// ===================================================
// 🏁 RUN
// ===================================================
app.Run();
