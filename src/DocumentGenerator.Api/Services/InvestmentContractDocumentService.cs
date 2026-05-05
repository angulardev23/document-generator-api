using System.Text.Json;
using DocumentGenerator.Api.Configuration;
using DocumentGenerator.Api.Contracts;
using DocumentGenerator.Application.Documents;
using DocumentGenerator.Application.Exceptions;
using DocumentGenerator.Domain.Services;
using Microsoft.Extensions.Options;

namespace DocumentGenerator.Api.Services;

public sealed class InvestmentContractDocumentService(
    IOptions<InvestmentContractOptions> options,
    IDocumentGenerationUseCase documentGenerationUseCase,
    IWordToPdfConverterService pdfConverter) : IInvestmentContractDocumentService
{
    private const string InvestmentContractTemplateFileName = "InvestmentContract.docx";
    private static readonly string InvestmentContractTemplatePath = Path.Combine(
        AppContext.BaseDirectory,
        "templates",
        InvestmentContractTemplateFileName);

    public async Task<GeneratedDocumentResponse> GenerateAsync(
        GenerateInvestmentContractRequest request,
        CancellationToken cancellationToken)
    {
        var templateContent = await File.ReadAllBytesAsync(InvestmentContractTemplatePath, cancellationToken);

        var templateData = new GenerateInvestmentContractTemplateData
        {
            ContractDate = request.ContractDate,
            LenderFullName = request.LenderFullName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CompanyName = request.CompanyName,
            InvestmentAmount = request.InvestmentAmount,
            EquityPercentage = request.EquityPercentage,
            BorrowerCompanyName = options.Value.BorrowerCompanyName,
            BorrowerCompanyAddress = options.Value.BorrowerCompanyAddress,
            BorrowerRegisterNumber = options.Value.BorrowerRegisterNumber
        };

        var command = new GenerateDocumentCommand(
            InvestmentContractTemplateFileName,
            templateContent,
            JsonSerializer.Serialize(templateData));

        var generatedDocument = await documentGenerationUseCase.GenerateAsync(command, cancellationToken);

        return await ConvertToPdfAsync(generatedDocument, cancellationToken);
    }

    private async Task<GeneratedDocumentResponse> ConvertToPdfAsync(
        GeneratedDocumentResponse response,
        CancellationToken cancellationToken)
    {
        try
        {
            response.Content.Position = 0;
            var generatedPdf = await pdfConverter.ConvertAsync(response.Content, cancellationToken);
            generatedPdf.Content.Position = 0;

            return new GeneratedDocumentResponse(
                generatedPdf.Content,
                generatedPdf.ContentType,
                Path.ChangeExtension(response.FileName, ".pdf"));
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DocumentProcessingException("PDF conversion failed.", exception);
        }
    }
}
