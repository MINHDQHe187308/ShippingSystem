using ASP.Models;
using ASP.Models.ASPModel;
using ASP.Models.Front;
using ASP.Service;
using ASP.Service.Implentations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DensoWorkerService;
using Serilog;
using Microsoft.AspNetCore.SignalR;
using ASP.Hubs;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.UseSerilog();

builder.ConfigureServices((hostContext, services) =>
{
    // DbContext
    services.AddDbContext<ASPDbContext>(options =>
        options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection")));

    // SignalR cho HubContext
    services.AddSignalR();
    // HttpClient và Scoped
    services.AddHttpClient<ExternalApiServiceInterface, ExternalApiService>();
    services.AddScoped<OrderServiceInterface, OrderService>();
    services.AddScoped<OrderRepositoryInterface, OrderRepository>();
    services.AddScoped<ShippingScheduleRepositoryInterface, ShippingScheduleRepository>();
    services.AddScoped<LeadtimeMasterRepositoryInterface, LeadtimeRepository>();
    services.AddMemoryCache();
    services.AddHostedService<Worker>();

    // Set ShutdownTimeout
    services.Configure<HostOptions>(options =>
    {
        options.ShutdownTimeout = TimeSpan.FromSeconds(120);
    });
});

await builder.Build().RunAsync();