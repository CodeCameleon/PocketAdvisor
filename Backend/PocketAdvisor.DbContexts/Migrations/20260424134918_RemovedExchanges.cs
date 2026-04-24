using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PocketAdvisor.DbContexts.Migrations;

/// <inheritdoc />
public partial class RemovedExchanges
    : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Exchanges"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Exchanges",
            columns: table => new
            {
                Base = table.Column<int>(type: "integer", nullable: false),
                Target = table.Column<int>(type: "integer", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Rate = table.Column<decimal>(type: "numeric", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Exchanges", x => new { x.Base, x.Target, x.Date });
            }
        );
    }
}
