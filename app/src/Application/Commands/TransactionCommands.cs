using Domain.Entities;
using Domain.Enums;

namespace Application.Commands;

public class TransactionCommands 
{
    private readonly ITransactionRepository _transactionRepository;

    public TransactionCommands(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Guid> InsertAsync(Guid sourceAccountId, Guid targetAccountId, int transferTypeId, decimal value, TransactionStatus status = default)
    {
        if (value <= 0) throw new ArgumentException("Value must be greater than zero.", nameof(value));
        if (sourceAccountId == Guid.Empty) throw new ArgumentException("Invalid sourceAccountId.", nameof(sourceAccountId));
        if (targetAccountId == Guid.Empty) throw new ArgumentException("Invalid targetAccountId.", nameof(targetAccountId));

        var externalId = Guid.NewGuid();
        var transaction = new Transaction(externalId, sourceAccountId, targetAccountId, transferTypeId, value, status);

        await _transactionRepository.AddAsync(transaction);
        return externalId;
    }
}