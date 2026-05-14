using DocumentGenerator.Domain;
using DocumentGenerator.Domain.Documents;
using DocumentGenerator.Domain.InvestmentContracts;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerator.Infrastructure.Persistence;

public sealed class DocumentGeneratorDbContext(DbContextOptions<DocumentGeneratorDbContext> options)
    : DbContext(options)
{
    public DbSet<StoredDocument> Documents => Set<StoredDocument>();

    public DbSet<InvestmentContract> InvestmentContracts => Set<InvestmentContract>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentGeneratorDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();

        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
