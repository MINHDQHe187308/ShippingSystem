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

    //  SignalR cho HubContext (dùng server-side)
    services.AddSignalR();
    services.AddScoped<IHubContext<OrderHub>>();

    // HttpClient và Scoped
    services.AddHttpClient<ExternalApiServiceInterface, ExternalApiService>();
    services.AddScoped<OrderServiceInterface, OrderService>();
    services.AddScoped<OrderRepositoryInterface, OrderRepository>();  // ??m b?o Scoped cho inject HubContext
    services.AddHostedService<Worker>();
});

await builder.Build().RunAsync();