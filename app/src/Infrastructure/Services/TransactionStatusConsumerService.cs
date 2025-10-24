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
        _logger.LogInformation("Listening to transaction-status-events");

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
                    _logger.LogWarning("Waiting for topic transaction-status-events...");
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
            Console.WriteLine("MESSAGE RECEIVED FROM KAFKA");
            Console.WriteLine(message.Value);

            var statusEvent = JsonSerializer.Deserialize<TransactionStatusEvent>(message.Value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (statusEvent == null)
            {
                _logger.LogWarning("Failed to deserialize transaction status event");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var transactionService = scope.ServiceProvider.GetRequiredService<ITransactionService>();

            await transactionService.UpdateTransactionStatusAsync(
                statusEvent.TransactionId,
                statusEvent.Status,
                statusEvent.Reason
            );

            _logger.LogInformation("Transaction {TransactionId} updated to {Status}", 
                statusEvent.TransactionId, statusEvent.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction {MessageKey}", message.Key);
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