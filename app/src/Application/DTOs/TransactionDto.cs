namespace Application.DTOs;

public class TransactionDto
{
    public Guid TransactionExternalId { get; set; }
    public Guid SourceAccountId { get; set; }
    public Guid TargetAccountId { get; set; }
    public int TransferTypeId { get; set; }
    public decimal Value { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}