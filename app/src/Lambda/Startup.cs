using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Application.Commands;
using Application.Services;
using Infrastructure.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Events;
using Infrastructure.Services;

public class Startup
{
    public IServiceProvider Configure()
    {    
        var services = new ServiceCollection();

        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        var conn = configuration.GetConnectionString("DefaultConnection")
                   ?? configuration["ConnectionStrings:DefaultConnection"]
                   ?? Environment.GetEnvironmentVariable("DefaultConnection");

        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException("Database connection string not configured.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(conn));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IEventPublisher, KafkaEventPublisher>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<TransactionCommands>();

        return services.BuildServiceProvider();
    }
}
