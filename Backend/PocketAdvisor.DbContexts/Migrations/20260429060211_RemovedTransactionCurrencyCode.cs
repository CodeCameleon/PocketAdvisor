using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PocketAdvisor.DbContexts.Migrations;

/// <inheritdoc />
public partial class RemovedTransactionCurrencyCode
    : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CurrencyCode",
            table: "Transactions"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CurrencyCode",
            table: "Transactions",
            type: "integer",
            nullable: false,
            defaultValue: 0
        );
    }
}
