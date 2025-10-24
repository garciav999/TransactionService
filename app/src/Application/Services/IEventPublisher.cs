using Domain.Events;

namespace Application.Services;

public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : DomainEvent;
    Task PublishAsync<T>(T domainEvent, string topic, CancellationToken cancellationToken = default) where T : DomainEvent;
}