using Microsoft.EntityFrameworkCore;
using DocumentGenerator.Domain.InvestmentContracts;

namespace DocumentGenerator.Infrastructure.Persistence;

public sealed class InvestmentContractRepository(
    DocumentGeneratorDbContext dbContext) : IInvestmentContractRepository
{
    public async Task AddAsync(
        InvestmentContract contract,
        CancellationToken cancellationToken)
    {
        await dbContext.InvestmentContracts.AddAsync(contract, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<InvestmentContract?> GetBySignWellDocumentIdAsync(
        string signWellDocumentId,
        CancellationToken cancellationToken)
    {
        return dbContext.InvestmentContracts
            .SingleOrDefaultAsync(
                contract => contract.SignWellDocumentId == signWellDocumentId,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
