using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task<Transaction?> GetByExternalIdAsync(Guid transactionExternalId)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionExternalId == transactionExternalId);
    }

    public async Task UpdateStatusAsync(Guid transactionExternalId, TransactionStatus status, string? reason = null)
    {
        var transaction = await GetByExternalIdAsync(transactionExternalId);
        if (transaction != null)
        {
            typeof(Transaction)
                .GetProperty(nameof(Transaction.Status))!
                .SetValue(transaction, status);

            await _context.SaveChangesAsync();
        }
        else
        {
            throw new InvalidOperationException($"Transaction with external ID {transactionExternalId} not found");
        }
    }

}