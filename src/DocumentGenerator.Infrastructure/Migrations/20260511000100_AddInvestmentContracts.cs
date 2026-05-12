using System;
using DocumentGenerator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentGenerator.Infrastructure.Migrations;

[Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(DocumentGeneratorDbContext))]
[Migration("20260511000100_AddInvestmentContracts")]
public partial class AddInvestmentContracts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "investment_contracts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                listing_id = table.Column<string>(type: "text", nullable: false),
                user_id = table.Column<string>(type: "text", nullable: false),
                sign_well_document_id = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_investment_contracts", x => x.id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "investment_contracts");
    }
}
