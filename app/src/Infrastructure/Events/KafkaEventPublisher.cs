using Application.Services;
using Confluent.Kafka;
using Domain.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Events;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly Dictionary<string, string> _topicMappings;

    public KafkaEventPublisher(IConfiguration configuration, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;

        // Configuración de Kafka
        var config = new ProducerConfig
        {
            BootstrapServers = configuration.GetConnectionString("Kafka") ?? "localhost:9092",
            ClientId = "transaction-service",
            Acks = Acks.All,
            MessageTimeoutMs = 10000,
            RequestTimeoutMs = 5000,
            EnableIdempotence = true,
            MaxInFlight = 1,
            CompressionType = CompressionType.Snappy
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Error}", e.Reason))
            .Build();

        // Mapeo de eventos a topics
        _topicMappings = new Dictionary<string, string>
        {
            { "transaction.created", "transaction-events" }
        };
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : DomainEvent
    {
        var topic = GetTopicForEvent(domainEvent.EventType);
        await PublishAsync(domainEvent, topic, cancellationToken);
    }

    public async Task PublishAsync<T>(T domainEvent, string topic, CancellationToken cancellationToken = default) where T : DomainEvent
    {
        try
        {
            var eventData = SerializeEvent(domainEvent);
            var key = GetEventKey(domainEvent);

            var message = new Message<string, string>
            {
                Key = key,
                Value = eventData,
                Headers = CreateHeaders(domainEvent)
            };

            _logger.LogInformation("Publishing event {EventType} to topic {Topic}", 
                domainEvent.EventType, topic);

            var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation("Event {EventType} published successfully. Partition: {Partition}, Offset: {Offset}",
                domainEvent.EventType, deliveryResult.Partition.Value, deliveryResult.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to topic {Topic}", 
                domainEvent.EventType, topic);
            throw;
        }
    }

    private string SerializeEvent<T>(T domainEvent) where T : DomainEvent
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(domainEvent, options);
    }

    private string GetEventKey<T>(T domainEvent) where T : DomainEvent
    {
        // Para TransactionCreatedEvent, usamos el TransactionExternalId como key
        if (domainEvent is TransactionCreatedEvent transactionEvent)
        {
            return transactionEvent.TransactionExternalId.ToString();
        }
        
        return domainEvent.Id.ToString();
    }

    private Headers CreateHeaders<T>(T domainEvent) where T : DomainEvent
    {
        var headers = new Headers
        {
            { "event-type", System.Text.Encoding.UTF8.GetBytes(domainEvent.EventType) },
            { "event-id", System.Text.Encoding.UTF8.GetBytes(domainEvent.Id.ToString()) },
            { "occurred-at", System.Text.Encoding.UTF8.GetBytes(domainEvent.OccurredAt.ToString("O")) },
            { "source-service", System.Text.Encoding.UTF8.GetBytes("transaction-service") }
        };

        return headers;
    }

    private string GetTopicForEvent(string eventType)
    {
        if (_topicMappings.TryGetValue(eventType, out var topic))
        {
            return topic;
        }

        _logger.LogWarning("No topic mapping found for event type {EventType}, using default topic", eventType);
        return "default-events";
    }

    public void Dispose()
    {
        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Kafka producer");
        }
    }
}