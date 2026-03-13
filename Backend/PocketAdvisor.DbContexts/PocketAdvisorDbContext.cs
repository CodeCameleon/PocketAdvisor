using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PocketAdvisor.Entities;

namespace PocketAdvisor.DbContexts;

/// <summary>
/// The database context for the project.
/// </summary>
public sealed class PocketAdvisorDbContext
    : DbContext
{
    #region Constants
    
    /// <summary>
    /// The database column type used for whole number values.
    /// </summary>
    private const string IntegerType = "INTEGER";
    
    /// <summary>
    /// The value converter used for converting decimal values to long values.
    /// </summary>
    private static readonly ValueConverter<decimal, long> DecimalToLongConverter = new(
        d => (long)(Math.Round(d, 2, MidpointRounding.AwayFromZero) * 100),
        l => l / 100m
    );
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PocketAdvisorDbContext" /> class,
    /// using the specified options.
    /// </summary>
    /// <param name="options">The options to be used by the context.</param>
    public PocketAdvisorDbContext(DbContextOptions<PocketAdvisorDbContext> options) : base(options) { }
    
    #endregion
    
    #region DbSets
    
    /// <summary>
    /// The database table for the account entities.
    /// </summary>
    public DbSet<Account> Accounts { get; set; }
    
    /// <summary>
    /// The database table for the category entities.
    /// </summary>
    public DbSet<Category> Categories { get; set; }
    
    /// <summary>
    /// The database table for the item entities.
    /// </summary>
    public DbSet<Item> Items { get; set; }
    
    /// <summary>
    /// The database table for the token entities.
    /// </summary>
    public DbSet<Token> Tokens { get; set; }
    
    /// <summary>
    /// The database table for the transaction entities.
    /// </summary>
    public DbSet<Transaction> Transactions { get; set; }
    
    /// <summary>
    /// The database table for the user entities.
    /// </summary>
    public DbSet<User> Users { get; set; }
    
    #endregion
    
    #region OnModelCreating
    
    /// <summary>
    /// Configures the model that was discovered by convention from the entity types
    /// exposed in <see cref="DbSet{TEntity}" /> properties on your derived context.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasIndex(a => new { a.Name, a.UserId }).IsUnique();
            entity.HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(a => a.Balance)
                .HasConversion(DecimalToLongConverter)
                .HasColumnType(IntegerType);
        });
        
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => new { c.Name, c.UserId }).IsUnique();
            entity.HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasIndex(i => new { i.Name, i.UserId }).IsUnique();
            entity.HasOne(i => i.User)
                .WithMany(u => u.Items)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasIndex(t => t.Hash).IsUnique();
            entity.HasOne(t => t.User)
                .WithMany(u => u.Tokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.FromAccount)
                .WithMany(a => a.OutgoingTransactions)
                .HasForeignKey(t => t.FromAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.ToAccount)
                .WithMany(a => a.IncomingTransactions)
                .HasForeignKey(t => t.ToAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
    
    #endregion
}
