using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Application.Services;

namespace Application.Commands;

public class TransactionCommands 
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IEventPublisher _eventPublisher;

    public TransactionCommands(ITransactionRepository transactionRepository, IEventPublisher eventPublisher)
    {
        _transactionRepository = transactionRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> InsertAsync(Guid sourceAccountId, Guid targetAccountId, int transferTypeId, decimal value, TransactionStatus status = TransactionStatus.Pending)
    {
        if (value <= 0) throw new ArgumentException("Value must be greater than zero.", nameof(value));
        if (sourceAccountId == Guid.Empty) throw new ArgumentException("Invalid sourceAccountId.", nameof(sourceAccountId));
        if (targetAccountId == Guid.Empty) throw new ArgumentException("Invalid targetAccountId.", nameof(targetAccountId));

        var externalId = Guid.NewGuid();
        var transaction = new Transaction(externalId, sourceAccountId, targetAccountId, transferTypeId, value, status);

        await _transactionRepository.AddAsync(transaction);

        var transactionCreatedEvent = new TransactionCreatedEvent(
            externalId,
            sourceAccountId,
            targetAccountId,
            transferTypeId,
            value,
            status.ToString()
        );

        await _eventPublisher.PublishAsync(transactionCreatedEvent);

        return externalId;
    }
}