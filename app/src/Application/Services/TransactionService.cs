using Domain.Enums;

namespace Application.Services;

public interface ITransactionService
{
    Task UpdateTransactionStatusAsync(Guid transactionExternalId, string status, string? reason = null);
}

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;

    public TransactionService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task UpdateTransactionStatusAsync(Guid transactionExternalId, string status, string? reason = null)
    {
        var transaction = await _transactionRepository.GetByExternalIdAsync(transactionExternalId);
        
        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction with external ID {transactionExternalId} not found");
        }

        if (!Enum.TryParse<TransactionStatus>(status, true, out var transactionStatus))
        {
            throw new ArgumentException($"Invalid transaction status: {status}");
        }

        await _transactionRepository.UpdateStatusAsync(transactionExternalId, transactionStatus, reason);
    }
}