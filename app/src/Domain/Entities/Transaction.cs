using Domain.Enums;

namespace Domain.Entities;

public class Transaction
{
    public Guid TransactionExternalId { get; private set; }
    public Guid SourceAccountId { get; private set; }
    public Guid TargetAccountId { get; private set; }
    public int TransferTypeId { get; private set; }
    public decimal Value { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Transaction(Guid transactionExternalId, Guid sourceAccountId, Guid targetAccountId, int transferTypeId, decimal value, TransactionStatus status = default)
    {
        TransactionExternalId = transactionExternalId;
        SourceAccountId = sourceAccountId;
        TargetAccountId = targetAccountId;
        TransferTypeId = transferTypeId;
        Value = value;
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }
}
