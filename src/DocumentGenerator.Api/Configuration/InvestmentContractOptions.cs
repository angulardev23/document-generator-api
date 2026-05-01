using System.ComponentModel.DataAnnotations;

namespace DocumentGenerator.Api.Configuration;

public sealed class InvestmentContractOptions
{
    public const string SectionName = "InvestmentContract";

    [Required]
    public string BorrowerCompanyName { get; init; } = string.Empty;

    [Required]
    public string BorrowerCompanyAddress { get; init; } = string.Empty;

    [Required]
    public string BorrowerRegisterNumber { get; init; } = string.Empty;
}
