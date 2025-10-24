using Application.Services;
using Domain.Events;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Events;

public class ConsoleEventPublisher : IEventPublisher
{
    private readonly ILogger<ConsoleEventPublisher> _logger;

    public ConsoleEventPublisher(ILogger<ConsoleEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : DomainEvent
    {
        _logger.LogInformation("📨 EVENT PUBLISHED: {EventType} → topic: transaction-events", domainEvent.EventType);
        _logger.LogInformation("📊 Event ID: {EventId}", domainEvent.Id);
        _logger.LogInformation("⏰ Occurred At: {OccurredAt}", domainEvent.OccurredAt);
        
        if (domainEvent is TransactionCreatedEvent transactionEvent)
        {
            _logger.LogInformation("💰 Transaction: {TransactionId} - Amount: ${Value}", 
                transactionEvent.TransactionExternalId, transactionEvent.Value);
        }
        
        _logger.LogInformation("✅ Event sent to topic 'transaction-events' (ready for anti-fraud processing)");
        
        return Task.CompletedTask;
    }

    public Task PublishAsync<T>(T domainEvent, string topic, CancellationToken cancellationToken = default) where T : DomainEvent
    {
        _logger.LogInformation("📨 EVENT PUBLISHED to topic '{Topic}': {EventType}", topic, domainEvent.EventType);
        return PublishAsync(domainEvent, cancellationToken);
    }
}