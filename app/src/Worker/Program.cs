using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Application.Services;
using Infrastructure.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Services;

namespace TransactionService.Worker;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var conn = context.Configuration.GetConnectionString("DefaultConnection");
                
                services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(conn));
                services.AddScoped<ITransactionRepository, TransactionRepository>();
                services.AddScoped<ITransactionService, Application.Services.TransactionService>();
                services.AddHostedService<TransactionStatusConsumerService>();
            });

        await builder.Build().RunAsync();
    }
}