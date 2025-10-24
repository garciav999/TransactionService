using Domain.Entities;
using Domain.Enums;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction);
    Task<Transaction?> GetByExternalIdAsync(Guid transactionExternalId);
    Task UpdateStatusAsync(Guid transactionExternalId, TransactionStatus status, string? reason = null);
}
