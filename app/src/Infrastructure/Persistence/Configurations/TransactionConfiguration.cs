
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transaction");

        builder.HasKey(t => t.TransactionExternalId);

        builder.Property(t => t.TransactionExternalId)
            .IsRequired();

        builder.Property(t => t.SourceAccountId)
            .IsRequired();

        builder.Property(t => t.TargetAccountId)
            .IsRequired();

        builder.Property(t => t.TransferTypeId)
            .IsRequired();

        builder.Property(t => t.Value)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.CreatedAt)
            .IsRequired();
    }
}
