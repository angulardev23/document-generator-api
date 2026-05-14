namespace DocumentGenerator.Domain.InvestmentContracts;

public interface IInvestmentContractRepository
{
    Task AddAsync(
        InvestmentContract contract,
        CancellationToken cancellationToken);

    Task<InvestmentContract?> GetBySignWellDocumentIdAsync(
        string signWellDocumentId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
