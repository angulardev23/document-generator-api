namespace DocumentGenerator.Api.Contracts;

public sealed class GenerateInvestmentContractSignWellRequest
{
    public required string ContractDate { get; init; }

    public required string LenderFullName { get; init; }

    public required string LenderEmail { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string CompanyName { get; init; }

    public required string InvestmentAmount { get; init; }

    public required string EquityPercentage { get; init; }

    public string? RedirectUrl { get; init; }
}
