using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Domain.Events;
using Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services;

public class TransactionStatusConsumerService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionStatusConsumerService> _logger;

    public TransactionStatusConsumerService(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<TransactionStatusConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration.GetConnectionString("Kafka") ?? "localhost:9092",
            GroupId = "transaction-service-group",
            ClientId = "transaction-service-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e.Reason))
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("transaction-status-events");
        _logger.LogInformation("🎧 Listening to transaction-status-events");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (consumeResult?.Message != null)
                    {
                        await ProcessTransactionStatusAsync(consumeResult.Message);
                        _consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    _logger.LogWarning("⏳ Waiting for topic transaction-status-events...");
                    await Task.Delay(5000, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
        finally
        {
            _consumer.Close();
            _logger.LogInformation("Consumer stopped");
        }
    }

    private async Task ProcessTransactionStatusAsync(Message<string, string> message)
    {
        try
        {
            Console.WriteLine("\n" + new string('=', 70));
            Console.WriteLine("📨 MESSAGE RECEIVED FROM KAFKA");
            Console.WriteLine(new string('=', 70));
            Console.WriteLine($"🔑 Message Key: {message.Key}");
            Console.WriteLine($"📄 Message Value (Raw JSON):");
            Console.WriteLine(message.Value);
            Console.WriteLine(new string('=', 70));

            _logger.LogInformation("Processing transaction status update: {MessageKey}", message.Key);
            _logger.LogInformation("Raw message value: {MessageValue}", message.Value);

            var statusEvent = JsonSerializer.Deserialize<TransactionStatusEvent>(message.Value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // ← Ignora mayúsculas/minúsculas
            });

            if (statusEvent == null)
            {
                _logger.LogWarning("Failed to deserialize transaction status event");
                Console.WriteLine("❌ ERROR: Failed to deserialize JSON");
                return;
            }

            Console.WriteLine($"✅ Deserialized Successfully:");
            Console.WriteLine($"   TransactionId: {statusEvent.TransactionId}");
            Console.WriteLine($"   Status: {statusEvent.Status}");
            Console.WriteLine($"   Reason: {statusEvent.Reason}");
            Console.WriteLine(new string('=', 70) + "\n");

            _logger.LogInformation("Deserialized event - TransactionId: {TransactionId}, Status: {Status}", 
                statusEvent.TransactionId, statusEvent.Status);

            // Usar scope para resolver dependencias
            using var scope = _serviceProvider.CreateScope();
            var transactionService = scope.ServiceProvider.GetRequiredService<ITransactionService>();

            // Actualizar estado de la transacción en la base de datos
            await transactionService.UpdateTransactionStatusAsync(
                statusEvent.TransactionId,
                statusEvent.Status,
                statusEvent.Reason
            );

            _logger.LogInformation("Updated transaction {TransactionId} status to {Status}", 
                statusEvent.TransactionId, statusEvent.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction status event for message key: {MessageKey}", message.Key);
            throw;
        }
    }

    public override void Dispose()
    {
        try
        {
            _consumer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing consumer");
        }
        
        base.Dispose();
    }
}