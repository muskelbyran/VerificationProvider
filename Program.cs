using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using VerificationProvider.Data.Contexts;
using VerificationProvider.Interfaces;
using VerificationProvider.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        // No Key Vault integration anymore, using App Settings directly from Azure
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        // Fetch ServiceBusConnection from Azure App Settings
        var serviceBusConnection = configuration["ServiceBusConnection"];
        Console.WriteLine("Fetched ServiceBusConnection: " + serviceBusConnection);
        if (string.IsNullOrEmpty(serviceBusConnection))
        {
            throw new InvalidOperationException("ServiceBusConnection is missing in App Settings.");
        }

        Environment.SetEnvironmentVariable("ServiceBusConnection", serviceBusConnection);

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Fetch SqlServer connection string from Azure App Settings
        var sqlServerConnectionString = configuration["SqlServer"];
        services.AddDbContext<DataContext>(options => options.UseSqlServer(sqlServerConnectionString));

        // Register services
        services.AddScoped<IVerificationService, VerificationService>();
        services.AddScoped<IVerificationCleanerService, VerificationCleanerService>();
        services.AddScoped<IValidateVerificationCodeService, ValidateVerificationCodeService>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var migration = context.Database.GetPendingMigrations();
        if (migration != null && migration.Any())
        {
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"ERROR :: Program.cs - Migration of Database :: {ex.Message}");
    }
}

// Run the host
host.Run();
