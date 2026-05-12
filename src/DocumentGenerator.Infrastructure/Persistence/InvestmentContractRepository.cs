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
}
