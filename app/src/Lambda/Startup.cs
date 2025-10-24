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

        // Agregar sistema de logging básico
        services.AddLogging();

        var conn = configuration.GetConnectionString("DefaultConnection")
                   ?? configuration["ConnectionStrings:DefaultConnection"]
                   ?? System.Environment.GetEnvironmentVariable("DefaultConnection");

        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException("Database connection string not configured (DefaultConnection).");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(conn)
        );

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IEventPublisher, KafkaEventPublisher>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<TransactionCommands>();

        return services.BuildServiceProvider();
    }

    private static void ShowPostmanInstructions()
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("🚀 AWS LAMBDA TEST TOOL - TRANSACTION SERVICE");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("📬 POSTMAN CONFIGURATION:");
        Console.WriteLine("");
        Console.WriteLine("   🌐 URL: http://localhost:5050/2015-03-31/functions/function/invocations");
        Console.WriteLine("   📋 Method: POST");
        Console.WriteLine("   📄 Headers: Content-Type: application/json");
        Console.WriteLine("");
        Console.WriteLine("   📝 Body (raw JSON):");
        Console.WriteLine("   {");
        Console.WriteLine("     \"SourceAccountId\": \"11111111-1111-1111-1111-111111111111\",");
        Console.WriteLine("     \"TargetAccountId\": \"22222222-2222-2222-2222-222222222222\",");
        Console.WriteLine("     \"TransferTypeId\": 1,");
        Console.WriteLine("     \"Value\": 120.0");
        Console.WriteLine("   }");
        Console.WriteLine("");
        Console.WriteLine("   ✅ Alternative URLs to try:");
        Console.WriteLine("   • http://localhost:5050/lambda-function");
        Console.WriteLine("   • http://localhost:5050/");
        Console.WriteLine("");
        Console.WriteLine("   🎯 Expected Response: 200 OK with transaction details");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("🔧 Use the web interface at: http://localhost:5050");
        Console.WriteLine(new string('=', 60) + "\n");
    }
}
