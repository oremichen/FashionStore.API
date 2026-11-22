using FashionStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Id)
            .HasMaxLength(TransactionRules.IdMaximumLength)
            .HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(transaction => transaction.OrderId).HasMaxLength(TransactionRules.OrderIdMaximumLength).IsRequired();
        builder.Property(transaction => transaction.UserId).HasMaxLength(TransactionRules.UserIdMaximumLength);
        builder.Property(transaction => transaction.Reference).HasMaxLength(TransactionRules.ReferenceMaximumLength).IsRequired();
        builder.Property(transaction => transaction.TransactionType).HasMaxLength(TransactionRules.TransactionTypeMaximumLength).IsRequired();
        builder.Property(transaction => transaction.Status).HasMaxLength(TransactionRules.StatusMaximumLength).IsRequired();
        builder.Property(transaction => transaction.Provider).HasMaxLength(TransactionRules.ProviderMaximumLength).IsRequired();
        builder.Property(transaction => transaction.ProviderTransactionId).HasMaxLength(TransactionRules.ProviderTransactionIdMaximumLength);
        builder.Property(transaction => transaction.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(transaction => transaction.Currency).HasMaxLength(TransactionRules.CurrencyLength).IsRequired();
        builder.Property(transaction => transaction.Channel).HasMaxLength(TransactionRules.ChannelMaximumLength);
        builder.Property(transaction => transaction.GatewayResponseCode).HasMaxLength(TransactionRules.GatewayResponseCodeMaximumLength);
        builder.Property(transaction => transaction.GatewayResponseMessage).HasMaxLength(TransactionRules.GatewayResponseMessageMaximumLength);
        builder.Property(transaction => transaction.FailureReason).HasMaxLength(TransactionRules.FailureReasonMaximumLength);
        builder.Property(transaction => transaction.ProcessedAt).IsRequired(false);
        builder.Property(transaction => transaction.CreatedAt).HasDefaultValueSql("now()").IsRequired();
        builder.Property(transaction => transaction.UpdatedAt).IsRequired();
        builder.HasIndex(transaction => new { transaction.OrderId, transaction.TransactionType })
            .IsUnique()
            .HasDatabaseName("ux_transactions_orderid_transactiontype");
        builder.HasIndex(transaction => new { transaction.Provider, transaction.ProviderTransactionId })
            .IsUnique()
            .HasFilter("\"ProviderTransactionId\" IS NOT NULL")
            .HasDatabaseName("ux_transactions_provider_providertransactionid");
        builder.HasIndex(transaction => transaction.Reference);
        builder.HasOne(transaction => transaction.Order)
            .WithMany()
            .HasForeignKey(transaction => transaction.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(transaction => transaction.User)
            .WithMany()
            .HasForeignKey(transaction => transaction.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
