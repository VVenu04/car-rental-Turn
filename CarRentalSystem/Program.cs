using CarRentalSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using CarRentalSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure EF Core with SQL Server
builder.Services.AddDbContext<CarRentalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure())); // Added for resiliency

// Configure session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register services
builder.Services.AddMemoryCache(); // For caching
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<SiteSettingsService>();
builder.Services.AddScoped<NotificationService>();

// For accessing HttpContext from other services
builder.Services.AddHttpContextAccessor();

var app = builder.Build();


//  CULTURE SETTINGS 
//  Create a new culture based on US English to keep number/date formats.
var customCulture = new CultureInfo("en-US");

//  currency symbol  "Rs. ".
customCulture.NumberFormat.CurrencySymbol = "Rs. ";

// 3. Tell the application to use this new custom culture as the default.
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(customCulture),
    SupportedCultures = new[] { customCulture },
    SupportedUICultures = new[] { customCulture }
});


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable session middleware
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();