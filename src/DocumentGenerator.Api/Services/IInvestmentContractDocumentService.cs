using DocumentGenerator.Api.Contracts;
using DocumentGenerator.Application.Documents;

namespace DocumentGenerator.Api.Services;

public interface IInvestmentContractDocumentService
{
    Task<GeneratedDocumentResponse> GenerateAsync(
        GenerateInvestmentContractRequest request,
        CancellationToken cancellationToken);

    Task<GenerateInvestmentContractSignWellResponse> GenerateAndUploadToSignWellAsync(
        GenerateInvestmentContractSignWellRequest request,
        CancellationToken cancellationToken);
}
