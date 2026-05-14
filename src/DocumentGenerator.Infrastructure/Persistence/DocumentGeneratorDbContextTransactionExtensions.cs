using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DocumentGenerator.Infrastructure.Persistence;

public static class DocumentGeneratorDbContextTransactionExtensions
{
    public static async Task<IDbContextTransaction?> BeginSerializableTransactionIfRelationalAsync(
        this DocumentGeneratorDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return null;
        }

        return await BeginRelationalTransactionAsync(dbContext, cancellationToken);
    }

    private static Task<IDbContextTransaction> BeginRelationalTransactionAsync(
        DocumentGeneratorDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
    }
}
