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

var builder = Host.CreateDefaultBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()   
    .CreateLogger();

builder.UseSerilog();

builder.ConfigureServices((hostContext, services) =>
{
    services.AddDbContext<ASPDbContext>(options =>
        options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection")));
    services.AddHttpClient<ExternalApiServiceInterface, ExternalApiService>();
    services.AddScoped<OrderServiceInterface, OrderService>();
    services.AddScoped<OrderRepositoryInterface , OrderRepository>();
    services.AddHostedService<Worker>();
});

await builder.Build().RunAsync();