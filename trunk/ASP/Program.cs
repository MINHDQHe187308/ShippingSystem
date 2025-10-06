using ASP.BaseCommon;
using ASP.Hubs;
using ASP.Models.Admin.Accounts;
using ASP.Models.Admin.Auths;
using ASP.Models.Admin.Logs;
using ASP.Models.Admin.Menus;
using ASP.Models.Admin.Roles;
using ASP.Models.Admin.ThemeOptions;
using ASP.Models.ASPModel;
using ASP.Models.Front;
using ASP.Policies;
using ASP.SeedData;
using ASP.Utilss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using ReflectionIT.Mvc.Paging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
#region signalR
builder.Services.AddSignalR();
#endregion
#region serilog
var logger = new LoggerConfiguration()
  .ReadFrom.Configuration(builder.Configuration)
  .Enrich.FromLogContext()
  .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);
#endregion
// Add services to the container.
builder.Services.AddControllersWithViews();
#region connection database
builder.Services.AddDbContextPool<ASPDbContext>(
    options => { options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")); }, poolSize: 32);
#endregion
#region pagination 
builder.Services.AddPaging(options =>
{
    options.ViewName = "Bootstrap4";
    options.SortExpressionParameterName = "sort";
    options.HtmlIndicatorUp = "<span class='pl-2'><i class='fas fa-sort-up'></i></span>";
    options.HtmlIndicatorDown = "<span class='pl-2'><i class='fas fa-sort-down'></i></span>";
});
#endregion
#region dependency injection 
builder.Services.AddScoped<BaseController>();
//backend  
builder.Services.AddTransient<LogRepositoryInterface, LogRepository>();
builder.Services.AddScoped<AccountRepositoryInterface, AccountRepository>();
builder.Services.AddScoped<RoleRepositoryInterface, RoleRepository>();
builder.Services.AddScoped<ThemeOptionRepositoryInterface, ThemeOptionRepository>();
builder.Services.AddScoped<AuthRepositoryInterface, AuthRepository>();
builder.Services.AddScoped<MenuRepositoryInterface, MenuRepository>();
builder.Services.AddScoped<CustomerRepositoryInterface, CustomerRepository>();
builder.Services.AddScoped<OrderRepositoryInterface, OrderRepository>();
builder.Services.AddScoped<OrderDetailRepositoryInterface, OrderDetailRepository>();
// frontend
//policies
builder.Services.AddSingleton<IAuthorizationHandler, UserPolicyAuthorizationHandler>();
#endregion
builder.Services.AddRazorPages();
#region config login

builder.Services.AddIdentity<ApplicationUser, Role>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    // Password settings.
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 3;
    options.Password.RequiredUniqueChars = 1;
    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    // User settings.
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = false;
    //
    
}).AddEntityFrameworkStores<ASPDbContext>().AddDefaultTokenProviders();
//
builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(120);
    //
    options.LoginPath = "/admin";
    options.AccessDeniedPath = "/Page404";
    options.SlidingExpiration = true;
    
});
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromSeconds(5);
});
#endregion

#region language config
builder.Services.Configure<RequestLocalizationOptions>(options => { 
    var supportedCultures = new [] { "en", "vn" };
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);

    options.DefaultRequestCulture.Culture.NumberFormat.NumberDecimalSeparator = ".";
    options.DefaultRequestCulture.Culture.NumberFormat.CurrencyDecimalSeparator = ".";
});
#endregion

var app = builder.Build();

//#region seed data
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var dbContext = services.GetRequiredService<ASPDbContext>();

//    await dbContext.Database.MigrateAsync();
//    await ApplicationUsersSeeder.SeedRolesAndAdminAsyn(services);
//}
//#endregion

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.UseRequestLocalization();
ResourceValidator.ValidateResourceKeys(typeof(Register));
#region route config
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    endpoints.MapControllerRoute(
        name: "Admin",
        pattern: "admin/{controller=Auth}/{action=Index}/{id?}");
});
#endregion

app.MapHub<PrivacyHub>("/privacyHub");
app.Run();