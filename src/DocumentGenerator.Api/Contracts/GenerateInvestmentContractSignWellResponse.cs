namespace DocumentGenerator.Api.Contracts;

public sealed record GenerateInvestmentContractSignWellResponse(
    string DocumentId,
    string SignWellUrl);
