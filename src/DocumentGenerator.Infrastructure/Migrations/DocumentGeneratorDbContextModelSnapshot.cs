using System;
using DocumentGenerator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentGenerator.Infrastructure.Migrations;

[DbContext(typeof(DocumentGeneratorDbContext))]
partial class DocumentGeneratorDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.0");

        modelBuilder.Entity("DocumentGenerator.Domain.Documents.StoredDocument", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<byte[]>("Content")
                    .IsRequired()
                    .HasColumnType("bytea")
                    .HasColumnName("content");

                b.Property<string>("ContentType")
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("content_type");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at");

                b.Property<string>("FileName")
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("file_name");

                b.Property<DateTime>("UpdatedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("updated_at");

                b.HasKey("Id");

                b.ToTable("documents");
            });

        modelBuilder.Entity("DocumentGenerator.Domain.InvestmentContracts.InvestmentContract", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedNever()
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at");

                b.Property<int>("ListingId")
                    .IsRequired()
                    .HasColumnType("integer")
                    .HasColumnName("listing_id");

                b.Property<string>("SignWellDocumentId")
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("sign_well_document_id");

                b.Property<Guid?>("SignedDocumentId")
                    .HasColumnType("uuid")
                    .HasColumnName("signed_document_id");

                b.Property<DateTime?>("SignedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("signed_at");

                b.Property<string>("Status")
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasColumnType("character varying(64)")
                    .HasColumnName("status");

                b.Property<DateTime>("UpdatedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("updated_at");

                b.Property<int>("UserId")
                    .IsRequired()
                    .HasColumnType("integer")
                    .HasColumnName("user_id");

                b.HasKey("Id");

                b.HasIndex("SignWellDocumentId")
                    .IsUnique();

                b.HasIndex("SignedDocumentId");

                b.ToTable("investment_contracts");
            });

        modelBuilder.Entity("DocumentGenerator.Domain.InvestmentContracts.InvestmentContract", b =>
            {
                b.HasOne("DocumentGenerator.Domain.Documents.StoredDocument", "SignedDocument")
                    .WithMany()
                    .HasForeignKey("SignedDocumentId")
                    .OnDelete(DeleteBehavior.Restrict);

                b.Navigation("SignedDocument");
            });
#pragma warning restore 612, 618
    }
}
