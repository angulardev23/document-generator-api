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

                b.ToTable("investment_contracts");
            });
#pragma warning restore 612, 618
    }
}
