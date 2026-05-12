namespace DocumentGenerator.Domain.InvestmentContracts;

public interface IInvestmentContractRepository
{
    Task AddAsync(
        InvestmentContract contract,
        CancellationToken cancellationToken);
}
