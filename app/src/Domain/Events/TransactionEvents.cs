namespace Domain.Events;

public abstract class DomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType { get; } = string.Empty;

    protected DomainEvent(string eventType)
    {
        EventType = eventType;
    }
}

public class TransactionCreatedEvent : DomainEvent
{
    public Guid TransactionExternalId { get; }
    public Guid SourceAccountId { get; }
    public Guid TargetAccountId { get; }
    public int TransferTypeId { get; }
    public decimal Value { get; }
    public string Status { get; }

    public TransactionCreatedEvent(
        Guid transactionExternalId,
        Guid sourceAccountId,
        Guid targetAccountId,
        int transferTypeId,
        decimal value,
        string status) : base("transaction.created")
    {
        TransactionExternalId = transactionExternalId;
        SourceAccountId = sourceAccountId;
        TargetAccountId = targetAccountId;
        TransferTypeId = transferTypeId;
        Value = value;
        Status = status;
    }
}

public class TransactionStatusEvent
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string Source { get; set; } = string.Empty;
}