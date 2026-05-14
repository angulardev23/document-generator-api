using System;
using DocumentGenerator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentGenerator.Infrastructure.Migrations;

[Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(DocumentGeneratorDbContext))]
[Migration("20260514000100_AddStoredDocumentsAndSignedContracts")]
public partial class AddStoredDocumentsAndSignedContracts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "documents",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                file_name = table.Column<string>(type: "text", nullable: false),
                content_type = table.Column<string>(type: "text", nullable: false),
                content = table.Column<byte[]>(type: "bytea", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_documents", x => x.id);
            });

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'investment_contracts'
                      AND column_name = 'listing_id'
                      AND data_type <> 'integer'
                ) THEN
                    ALTER TABLE investment_contracts
                    ALTER COLUMN listing_id TYPE integer
                    USING listing_id::integer;
                END IF;
            END $$;
            """);

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'investment_contracts'
                      AND column_name = 'user_id'
                      AND data_type <> 'integer'
                ) THEN
                    ALTER TABLE investment_contracts
                    ALTER COLUMN user_id TYPE integer
                    USING user_id::integer;
                END IF;
            END $$;
            """);

        migrationBuilder.AddColumn<Guid>(
            name: "signed_document_id",
            table: "investment_contracts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "signed_at",
            table: "investment_contracts",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_investment_contracts_sign_well_document_id",
            table: "investment_contracts",
            column: "sign_well_document_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_investment_contracts_signed_document_id",
            table: "investment_contracts",
            column: "signed_document_id");

        migrationBuilder.AddForeignKey(
            name: "fk_investment_contracts_documents_signed_document_id",
            table: "investment_contracts",
            column: "signed_document_id",
            principalTable: "documents",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_investment_contracts_documents_signed_document_id",
            table: "investment_contracts");

        migrationBuilder.DropIndex(
            name: "ix_investment_contracts_sign_well_document_id",
            table: "investment_contracts");

        migrationBuilder.DropIndex(
            name: "ix_investment_contracts_signed_document_id",
            table: "investment_contracts");

        migrationBuilder.DropColumn(
            name: "signed_document_id",
            table: "investment_contracts");

        migrationBuilder.DropColumn(
            name: "signed_at",
            table: "investment_contracts");

        migrationBuilder.DropTable(
            name: "documents");
    }
}
