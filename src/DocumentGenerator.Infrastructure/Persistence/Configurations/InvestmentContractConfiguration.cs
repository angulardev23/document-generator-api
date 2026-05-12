using DocumentGenerator.Domain.InvestmentContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerator.Infrastructure.Persistence.Configurations;

public sealed class InvestmentContractConfiguration
    : IEntityTypeConfiguration<InvestmentContract>
{
    public void Configure(EntityTypeBuilder<InvestmentContract> builder)
    {
        builder.ToTable("investment_contracts");

        builder.HasKey(contract => contract.Id);

        builder.Property(contract => contract.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(contract => contract.ListingId)
            .HasColumnName("listing_id")
            .IsRequired();

        builder.Property(contract => contract.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(contract => contract.SignWellDocumentId)
            .HasColumnName("sign_well_document_id")
            .IsRequired();

        builder.Property(contract => contract.Status)
            .HasColumnName("status")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(contract => contract.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(contract => contract.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
