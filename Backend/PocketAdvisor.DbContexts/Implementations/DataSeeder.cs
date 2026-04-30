using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PocketAdvisor.DbContexts.Interfaces;
using PocketAdvisor.Entities;
using PocketAdvisor.Enums;

namespace PocketAdvisor.DbContexts.Implementations;

/// <summary>
/// Represents the implementation for seeding initial data into the database.
/// </summary>
public sealed class DataSeeder
    : IDataSeeder
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSeeder" /> class.
    /// </summary>
    /// <param name="context">The database context instance.</param>
    /// <param name="passwordHasher">The password hasher for hashing user passwords.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public DataSeeder(PocketAdvisorDbContext context, IPasswordHasher<User> passwordHasher)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        
        Context = context;
        PasswordHasher = passwordHasher;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The database context instance.
    /// </summary>
    private PocketAdvisorDbContext Context { get; }
    
    /// <summary>
    /// The password hasher for hashing user passwords.
    /// </summary>
    private IPasswordHasher<User> PasswordHasher { get; }
    
    #endregion
    
    #region SeedAsync
    
    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await Context.Users.AnyAsync(cancellationToken))
        {
            return;
        }
        
        User adminUser = CreateUser("admin@pocketadvisor.dev", "Admin12!", EUserRole.Administrator);
        User regularUser = CreateUser("user@pocketadvisor.dev", "User123!", EUserRole.User);
        
        await Context.Users.AddRangeAsync(adminUser, regularUser);
        await Context.SaveChangesAsync(cancellationToken);
        
        Account bankAccount = CreateAccount("Bank", 350_000.00m, ECurrencyCode.Huf, regularUser);
        Account cashAccount = CreateAccount("Wallet", 15_000.00m, ECurrencyCode.Huf, regularUser);
        
        await Context.Accounts.AddRangeAsync(bankAccount, cashAccount);
        await Context.SaveChangesAsync(cancellationToken);
        
        List<Category> globalCategories =
        [
            CreateCategory("Clothing", null),
            CreateCategory("Dining Out", null),
            CreateCategory("Education", null),
            CreateCategory("Entertainment", null),
            CreateCategory("Groceries", null),
            CreateCategory("Healthcare", null),
            CreateCategory("Housing", null),
            CreateCategory("Transfers", null),
            CreateCategory("Transportation", null)
        ];
        
        Category clothing = globalCategories[0];
        Category diningOut = globalCategories[1];
        Category entertainment = globalCategories[3];
        Category groceries = globalCategories[4];
        Category housing = globalCategories[6];
        Category transfers = globalCategories[7];
        Category transport = globalCategories[8];
        
        List<Category> personalCategories =
        [
            CreateCategory("Gym & Sports", regularUser),
            CreateCategory("Side Projects", regularUser),
            CreateCategory("Subscriptions", regularUser)
        ];
        
        Category gymSports = personalCategories[0];
        Category subscriptions = personalCategories[2];
        
        await Context.Categories.AddRangeAsync([.. globalCategories, .. personalCategories]);
        await Context.SaveChangesAsync(cancellationToken);
        
        List<Item> items =
        [
            CreateItem("Bread", EUnitCategory.Mass, regularUser),
            CreateItem("Bus Ticket", EUnitCategory.Uncategorized, regularUser),
            CreateItem("Chicken Breast", EUnitCategory.Mass, regularUser),
            CreateItem("Cloud Storage", EUnitCategory.DataSize, regularUser),
            CreateItem("Coffee", EUnitCategory.Volume, regularUser),
            CreateItem("Eggs", EUnitCategory.Uncategorized, regularUser),
            CreateItem("Electricity", EUnitCategory.Energy, regularUser),
            CreateItem("Fabric", EUnitCategory.Length, regularUser),
            CreateItem("Flooring", EUnitCategory.Area, regularUser),
            CreateItem("Fuel", EUnitCategory.Volume, regularUser),
            CreateItem("Gym Membership", EUnitCategory.Time, regularUser),
            CreateItem("Milk", EUnitCategory.Volume, regularUser),
            CreateItem("Movie Ticket", EUnitCategory.Uncategorized, regularUser),
            CreateItem("Olive Oil", EUnitCategory.Volume, regularUser),
            CreateItem("Rice", EUnitCategory.Mass, regularUser),
            CreateItem("Streaming Service", EUnitCategory.Time, regularUser),
            CreateItem("Transfer", EUnitCategory.Uncategorized, regularUser)
        ];
        
        Item bread = items[0];
        Item busTicket = items[1];
        Item chicken = items[2];
        Item cloudStorage = items[3];
        Item coffee = items[4];
        Item eggs = items[5];
        Item electricity = items[6];
        Item fabric = items[7];
        Item flooring = items[8];
        Item fuel = items[9];
        Item gymMembership = items[10];
        Item milk = items[11];
        Item movieTicket = items[12];
        Item oliveOil = items[13];
        Item rice = items[14];
        Item streaming = items[15];
        Item transfer = items[16];
        
        await Context.Items.AddRangeAsync(items, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        
        DateTime now = DateTime.UtcNow;
        
        List<Transaction> transactions =
        [
            // 1 – Weekly grocery run
            CreateTransaction(Day(84), groceries, bankAccount, null),
            // 2 – Coffee stop
            CreateTransaction(Day(82), diningOut, cashAccount, null),
            // 3 – Bus commute
            CreateTransaction(Day(80), transport, cashAccount, null),
            // 4 – Grocery top-up
            CreateTransaction(Day(77), groceries, bankAccount, null),
            // 5 – Streaming subscription
            CreateTransaction(Day(75), subscriptions, bankAccount, null),
            // 6 – Gym membership
            CreateTransaction(Day(74), gymSports, bankAccount, null),
            // 7 – Lunch at restaurant
            CreateTransaction(Day(70), diningOut, cashAccount, null),
            // 8 – Fuel up
            CreateTransaction(Day(68), transport, bankAccount, null),
            // 9 – Big grocery shop
            CreateTransaction(Day(63), groceries, bankAccount, null),
            // 10 – Coffee
            CreateTransaction(Day(61), diningOut, cashAccount, null),
            // 11 – Bus tickets weekly batch
            CreateTransaction(Day(59), transport, cashAccount, null),
            // 12 – Movie night
            CreateTransaction(Day(56), entertainment, bankAccount, null),
            // 13 – Transfer: bank to wallet
            CreateTransaction(Day(55), transfers, bankAccount, cashAccount),
            // 14 – Fabric purchase
            CreateTransaction(Day(50), clothing, cashAccount, null),
            // 15 – Flooring purchase
            CreateTransaction(Day(48), housing, bankAccount, null),
            // 16 – Monthly streaming
            CreateTransaction(Day(45), subscriptions, bankAccount, null),
            // 17 – Gym membership
            CreateTransaction(Day(44), gymSports, bankAccount, null),
            // 18 – Grocery run
            CreateTransaction(Day(42), groceries, bankAccount, null),
            // 19 – Fuel
            CreateTransaction(Day(39), transport, bankAccount, null),
            // 20 – Restaurant dinner
            CreateTransaction(Day(35), diningOut, bankAccount, null),
            // 21 – Transfer: bank to wallet
            CreateTransaction(Day(32), transfers, bankAccount, cashAccount),
            // 22 – Electricity bill
            CreateTransaction(Day(30), housing, bankAccount, null),
            // 23 – Grocery mid-month
            CreateTransaction(Day(28), groceries, bankAccount, null),
            // 24 – Bus tickets
            CreateTransaction(Day(25), transport, cashAccount, null),
            // 25 – Coffee
            CreateTransaction(Day(22), diningOut, cashAccount, null),
            // 26 – Cloud storage plan
            CreateTransaction(Day(20), subscriptions, bankAccount, null),
            // 27 – Monthly streaming
            CreateTransaction(Day(15), subscriptions, bankAccount, null),
            // 28 – Gym membership
            CreateTransaction(Day(14), gymSports, bankAccount, null),
            // 29 – Weekly grocery shop
            CreateTransaction(Day(12), groceries, bankAccount, null),
            // 30 – Transfer: wallet to bank
            CreateTransaction(Day(10), transfers, cashAccount, bankAccount),
            // 31 – Movie tickets
            CreateTransaction(Day(7), entertainment, bankAccount, null),
            // 32 – Grocery run yesterday
            CreateTransaction(Day(1), groceries, bankAccount, null),
        ];
        
        await Context.Transactions.AddRangeAsync(transactions, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        
        List<TransactionItem> transactionItems =
        [
            // 1 – Weekly grocery run
            CreateTransactionItem(transactions[0], milk, 349m, 1m, EUnit.Liter),
            CreateTransactionItem(transactions[0], bread, 220m, 500m, EUnit.Gram),
            CreateTransactionItem(transactions[0], eggs, 450m, 12m, EUnit.Piece),
            CreateTransactionItem(transactions[0], rice, 280m, 1_000m, EUnit.Gram),
            // 2 – Coffee stop
            CreateTransactionItem(transactions[1], coffee, 380m, 0.350m, EUnit.Liter),
            // 3 – Bus commute
            CreateTransactionItem(transactions[2], busTicket, 300m, 2m, EUnit.Piece),
            // 4 – Grocery top-up
            CreateTransactionItem(transactions[3], chicken, 650m, 500m, EUnit.Gram),
            CreateTransactionItem(transactions[3], oliveOil, 520m, 0.500m, EUnit.Liter),
            // 5 – Streaming subscription
            CreateTransactionItem(transactions[4], streaming, 1_399m, 1m, EUnit.Month),
            // 6 – Gym membership
            CreateTransactionItem(transactions[5], gymMembership, 3_500m, 1m, EUnit.Month),
            // 7 – Lunch at restaurant
            CreateTransactionItem(transactions[6], chicken, 1_200m, 300m, EUnit.Gram),
            CreateTransactionItem(transactions[6], coffee, 380m, 0.250m, EUnit.Liter),
            // 8 – Fuel up
            CreateTransactionItem(transactions[7], fuel, 14_400m, 40m, EUnit.Liter),
            // 9 – Big grocery shop
            CreateTransactionItem(transactions[8], milk, 698m, 2m, EUnit.Liter),
            CreateTransactionItem(transactions[8], bread, 440m, 1_000m, EUnit.Gram),
            CreateTransactionItem(transactions[8], eggs, 900m, 24m, EUnit.Piece),
            CreateTransactionItem(transactions[8], chicken, 1_300m, 1_000m, EUnit.Gram),
            CreateTransactionItem(transactions[8], rice, 560m, 2_000m, EUnit.Gram),
            // 10 – Coffee
            CreateTransactionItem(transactions[9], coffee, 380m, 0.350m, EUnit.Liter),
            // 11 – Bus tickets weekly batch
            CreateTransactionItem(transactions[10], busTicket, 750m, 5m, EUnit.Piece),
            // 12 – Movie night
            CreateTransactionItem(transactions[11], movieTicket, 4_400m, 2m, EUnit.Piece),
            // 13 – Transfer: bank to wallet
            CreateTransactionItem(transactions[12], transfer, 20_000m, 1m, EUnit.Piece),
            // 14 – Fabric purchase
            CreateTransactionItem(transactions[13], fabric, 3_200m, 2m, EUnit.Meter),
            // 15 – Flooring purchase
            CreateTransactionItem(transactions[14], flooring, 45_000m, 15m, EUnit.SquareMeter),
            // 16 – Monthly streaming
            CreateTransactionItem(transactions[15], streaming, 1_399m, 1m, EUnit.Month),
            // 17 – Gym membership
            CreateTransactionItem(transactions[16], gymMembership, 3_500m, 1m, EUnit.Month),
            // 18 – Grocery run
            CreateTransactionItem(transactions[17], bread, 440m, 1_000m, EUnit.Gram),
            CreateTransactionItem(transactions[17], eggs, 450m, 12m, EUnit.Piece),
            CreateTransactionItem(transactions[17], oliveOil, 780m, 0.750m, EUnit.Liter),
            // 19 – Fuel
            CreateTransactionItem(transactions[18], fuel, 12_600m, 35m, EUnit.Liter),
            // 20 – Restaurant dinner
            CreateTransactionItem(transactions[19], chicken, 2_400m, 600m, EUnit.Gram),
            CreateTransactionItem(transactions[19], coffee, 560m, 0.500m, EUnit.Liter),
            // 21 – Transfer: bank to wallet
            CreateTransactionItem(transactions[20], transfer, 15_000m, 1m, EUnit.Piece),
            // 22 – Electricity bill
            CreateTransactionItem(transactions[21], electricity, 18_500m, 250m, EUnit.KilowattHour),
            // 23 – Grocery mid-month
            CreateTransactionItem(transactions[22], milk, 349m, 1m, EUnit.Liter),
            CreateTransactionItem(transactions[22], chicken, 975m, 750m, EUnit.Gram),
            CreateTransactionItem(transactions[22], rice, 280m, 1_000m, EUnit.Gram),
            // 24 – Bus tickets
            CreateTransactionItem(transactions[23], busTicket, 750m, 5m, EUnit.Piece),
            // 25 – Coffee
            CreateTransactionItem(transactions[24], coffee, 380m, 0.350m, EUnit.Liter),
            // 26 – Cloud storage plan
            CreateTransactionItem(transactions[25], cloudStorage, 990m, 200m, EUnit.Gigabyte),
            // 27 – Monthly streaming
            CreateTransactionItem(transactions[26], streaming, 1_399m, 1m, EUnit.Month),
            // 28 – Gym membership
            CreateTransactionItem(transactions[27], gymMembership, 3_500m, 1m, EUnit.Month),
            // 29 – Weekly grocery shop
            CreateTransactionItem(transactions[28], milk, 698m, 2m, EUnit.Liter),
            CreateTransactionItem(transactions[28], bread, 220m, 500m, EUnit.Gram),
            CreateTransactionItem(transactions[28], eggs, 450m, 12m, EUnit.Piece),
            // 30 – Transfer: wallet to bank
            CreateTransactionItem(transactions[29], transfer, 5_000m, 1m, EUnit.Piece),
            // 31 – Movie tickets
            CreateTransactionItem(transactions[30], movieTicket, 4_400m, 2m, EUnit.Piece),
            // 32 – Grocery run yesterday
            CreateTransactionItem(transactions[31], bread, 220m, 500m, EUnit.Gram),
            CreateTransactionItem(transactions[31], chicken, 650m, 500m, EUnit.Gram),
            CreateTransactionItem(transactions[31], oliveOil, 520m, 0.500m, EUnit.Liter),
            CreateTransactionItem(transactions[31], rice, 560m, 2_000m, EUnit.Gram),
        ];
        
        await Context.TransactionItems.AddRangeAsync(transactionItems, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        
        return;
        
        DateTime Day(int daysAgo) => now.AddDays(-daysAgo);
    }
    
    #endregion
    
    #region CreateAccount
    
    /// <summary>
    /// Creates a new account entity with the given parameters.
    /// </summary>
    /// <param name="name">The name of the account.</param>
    /// <param name="balance">The starting balance of the account.</param>
    /// <param name="currencyCode">The currency code of the account.</param>
    /// <param name="user">The user to whom the account belongs.</param>
    /// <returns>The constructed account entity.</returns>
    private static Account CreateAccount(string name, decimal balance, ECurrencyCode currencyCode, User user)
    {
        Account account = new()
        {
            Name = name,
            Balance = balance,
            CurrencyCode = currencyCode,
            UserId = user.Id,
        };
        
        return account;
    }
    
    #endregion
    
    #region CreateCategory
    
    /// <summary>
    /// Creates a new category entity with the given parameters.
    /// </summary>
    /// <param name="name">The name of the category.</param>
    /// <param name="user">
    /// The user to whom the category belongs, or <see langword="null" /> for global categories.
    /// </param>
    /// <returns>The constructed category entity.</returns>
    private static Category CreateCategory(string name, User? user)
    {
        Category category = new()
        {
            Name = name,
            UserId = user?.Id,
        };
        
        return category;
    }
    
    #endregion
    
    #region CreateItem
    
    /// <summary>
    /// Creates a new item entity with the given parameters.
    /// </summary>
    /// <param name="name">The name of the item.</param>
    /// <param name="unitCategory">The unit category of the item.</param>
    /// <param name="user">The user to whom the item belongs.</param>
    /// <returns>The constructed item entity.</returns>
    private static Item CreateItem(string name, EUnitCategory unitCategory, User user)
    {
        Item item = new()
        {
            Name = name,
            UnitCategory = unitCategory,
            UserId = user.Id,
        };
        
        return item;
    }
    
    #endregion
    
    #region CreateTransaction
    
    /// <summary>
    /// Create a new transaction entity with the given parameters.
    /// </summary>
    /// <param name="occurredAt">The occurrence date and time of the transaction.</param>
    /// <param name="category">The category to which the transaction belongs.</param>
    /// <param name="fromAccount">The account from which the transaction originated.</param>
    /// <param name="toAccount">The account to which the transaction is directed.</param>
    /// <returns>The constructed transaction entity.</returns>
    private static Transaction CreateTransaction(DateTime occurredAt, Category category,
        Account? fromAccount, Account? toAccount)
    {
        Transaction transaction = new()
        {
            OccurredAt = occurredAt,
            CategoryId = category.Id,
            FromAccountId = fromAccount?.Id,
            ToAccountId = toAccount?.Id
        };
        
        return transaction;
    }
    
    #endregion
    
    #region CreateTransactionItem
    
    /// <summary>
    /// Create a new transaction item entity with the given parameters.
    /// </summary>
    /// <param name="transaction">The transaction on which the item appears.</param>
    /// <param name="item">The item that is associated with the transaction.</param>
    /// <param name="totalPrice">The total price of the item at the time of the transaction.</param>
    /// <param name="amount">The amount of the item on the transaction.</param>
    /// <param name="unit">The unit of the item on the transaction.</param>
    /// <returns>The constructed transaction item entity.</returns>
    private static TransactionItem CreateTransactionItem(Transaction transaction, Item item,
        decimal totalPrice, decimal amount, EUnit unit)
    {
        TransactionItem transactionItem = new()
        {
            TransactionId = transaction.Id,
            ItemId = item.Id,
            TotalPrice = totalPrice,
            Amount = new(amount, unit),
        };
        
        return transactionItem;
    }
    
    #endregion
    
    #region CreateUser
    
    /// <summary>
    /// Create a new user entity with the given parameters.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="password">The password of the user.</param>
    /// <param name="role">The role of the user.</param>
    /// <returns>The constructed user entity.</returns>
    private User CreateUser(string email, string password, EUserRole role)
    {
        User user = new()
        {
            IsEmailVerified = true,
            Email = email,
            PasswordHash = string.Empty,
            Role = role,
        };
        user.PasswordHash = PasswordHasher.HashPassword(user, password);
        
        return user;
    }
    
    #endregion
}
