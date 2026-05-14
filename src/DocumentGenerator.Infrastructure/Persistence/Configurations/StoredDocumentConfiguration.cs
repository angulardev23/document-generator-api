using DocumentGenerator.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerator.Infrastructure.Persistence.Configurations;

public sealed class StoredDocumentConfiguration : IEntityTypeConfiguration<StoredDocument>
{
    public void Configure(EntityTypeBuilder<StoredDocument> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(document => document.FileName)
            .HasColumnName("file_name")
            .IsRequired();

        builder.Property(document => document.ContentType)
            .HasColumnName("content_type")
            .IsRequired();

        builder.Property(document => document.Content)
            .HasColumnName("content")
            .HasColumnType("bytea")
            .IsRequired();

        builder.Property(document => document.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(document => document.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
