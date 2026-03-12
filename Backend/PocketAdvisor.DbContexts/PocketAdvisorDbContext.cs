using Microsoft.EntityFrameworkCore;
using PocketAdvisor.Entities;

namespace PocketAdvisor.DbContexts;

/// <summary>
/// The database context for the project.
/// </summary>
public sealed class PocketAdvisorDbContext
    : DbContext
{
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
    /// The database table for the category entities.
    /// </summary>
    public DbSet<Category> Categories { get; set; }
    
    /// <summary>
    /// The database table for the token entities.
    /// </summary>
    public DbSet<Token> Tokens { get; set; }
    
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
        
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => new { c.Name, c.UserId }).IsUnique();
            entity.HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
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
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
    
    #endregion
}
