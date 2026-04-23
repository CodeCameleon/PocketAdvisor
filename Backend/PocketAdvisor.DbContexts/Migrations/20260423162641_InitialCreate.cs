using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PocketAdvisor.DbContexts.Migrations;

/// <inheritdoc />
public partial class InitialCreate
    : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
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

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Role = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            }
        );

        migrationBuilder.CreateTable(
            name: "Accounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                CurrencyCode = table.Column<int>(type: "integer", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Accounts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Accounts_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.Id);
                table.ForeignKey(
                    name: "FK_Categories_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "Items",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                UnitCategory = table.Column<int>(type: "integer", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Items", x => x.Id);
                table.ForeignKey(
                    name: "FK_Items_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "Tokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Hash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                ExpiryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_Tokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "Transactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CurrencyCode = table.Column<int>(type: "integer", nullable: false),
                CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                FromAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                ToAccountId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Transactions_Accounts_FromAccountId",
                    column: x => x.FromAccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_Transactions_Accounts_ToAccountId",
                    column: x => x.ToAccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_Transactions_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "TransactionItems",
            columns: table => new
            {
                TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                AmountValue = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                AmountUnit = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TransactionItems", x => new { x.TransactionId, x.ItemId });
                table.ForeignKey(
                    name: "FK_TransactionItems_Items_ItemId",
                    column: x => x.ItemId,
                    principalTable: "Items",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_TransactionItems_Transactions_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "Transactions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_Name_UserId",
            table: "Accounts",
            columns: ["Name", "UserId"],
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_UserId",
            table: "Accounts",
            column: "UserId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Name_UserId",
            table: "Categories",
            columns: ["Name", "UserId"],
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Categories_UserId",
            table: "Categories",
            column: "UserId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Items_Name_UserId",
            table: "Items",
            columns: ["Name", "UserId"],
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Items_UserId",
            table: "Items",
            column: "UserId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Tokens_Hash",
            table: "Tokens",
            column: "Hash",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Tokens_UserId",
            table: "Tokens",
            column: "UserId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_TransactionItems_ItemId",
            table: "TransactionItems",
            column: "ItemId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_CategoryId",
            table: "Transactions",
            column: "CategoryId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_FromAccountId",
            table: "Transactions",
            column: "FromAccountId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_ToAccountId",
            table: "Transactions",
            column: "ToAccountId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Exchanges"
        );

        migrationBuilder.DropTable(
            name: "Tokens"
        );

        migrationBuilder.DropTable(
            name: "TransactionItems"
        );

        migrationBuilder.DropTable(
            name: "Items"
        );

        migrationBuilder.DropTable(
            name: "Transactions"
        );

        migrationBuilder.DropTable(
            name: "Accounts"
        );

        migrationBuilder.DropTable(
            name: "Categories"
        );

        migrationBuilder.DropTable(
            name: "Users"
        );
    }
}
