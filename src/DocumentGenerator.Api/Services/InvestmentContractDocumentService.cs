using System.Text.Json;
using DocumentGenerator.Api.Configuration;
using DocumentGenerator.Api.Contracts;
using DocumentGenerator.Application.Documents;
using DocumentGenerator.Application.Exceptions;
using DocumentGenerator.Domain.InvestmentContracts;
using DocumentGenerator.Domain.Services;
using Microsoft.Extensions.Options;

namespace DocumentGenerator.Api.Services;

public sealed class InvestmentContractDocumentService(
    IOptions<InvestmentContractOptions> options,
    IDocumentGenerationUseCase documentGenerationUseCase,
    IWordToPdfConverterService pdfConverter,
    ISignWellClient signWellClient,
    IInvestmentContractRepository investmentContractRepository) : IInvestmentContractDocumentService
{
    private const string InvestmentContractTemplateFileName = "InvestmentContract.docx";
    private static readonly string InvestmentContractTemplatePath = Path.Combine(
        AppContext.BaseDirectory,
        "templates",
        InvestmentContractTemplateFileName);
    private static readonly Lazy<byte[]> InvestmentContractTemplateContent = new(
        () => File.ReadAllBytes(InvestmentContractTemplatePath),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public async Task<GeneratedDocumentResponse> GenerateAsync(
        GenerateInvestmentContractRequest request,
        CancellationToken cancellationToken)
    {
        return await GeneratePdfAsync(
            request.ContractDate,
            request.LenderFullName,
            request.FirstName,
            request.LastName,
            request.CompanyName,
            request.InvestmentAmount,
            request.EquityPercentage,
            cancellationToken);
    }

    public async Task<GenerateInvestmentContractSignWellResponse> GenerateAndUploadToSignWellAsync(
        GenerateInvestmentContractSignWellRequest request,
        CancellationToken cancellationToken)
    {
        var generatedPdf = await GeneratePdfAsync(
            request.ContractDate,
            request.LenderFullName,
            request.FirstName,
            request.LastName,
            request.CompanyName,
            request.InvestmentAmount,
            request.EquityPercentage,
            cancellationToken);

        try
        {
            generatedPdf.Content.Position = 0;

            var signWellResponse = await signWellClient.CreateDocumentAsync(
                new SignWellCreateDocumentRequest(
                    generatedPdf.FileName,
                    generatedPdf.Content,
                    request.LenderFullName,
                    request.LenderEmail,
                    request.RedirectUrl),
                cancellationToken);

            await investmentContractRepository.AddAsync(
                new InvestmentContract
                {
                    ListingId = request.ListingId,
                    UserId = request.UserId,
                    SignWellDocumentId = signWellResponse.DocumentId,
                    Status = InvestmentContract.PendingSignatureStatus
                },
                cancellationToken);

            return new GenerateInvestmentContractSignWellResponse(
                signWellResponse.DocumentId,
                signWellResponse.SignWellUrl);
        }
        catch (Exception exception) when (exception is not ValidationException)
        {
            throw new DocumentSigningException("SignWell upload failed.", exception);
        }
        finally
        {
            await generatedPdf.Content.DisposeAsync();
        }
    }

    private async Task<GeneratedDocumentResponse> GeneratePdfAsync(
        string contractDate,
        string lenderFullName,
        string firstName,
        string lastName,
        string companyName,
        string investmentAmount,
        string equityPercentage,
        CancellationToken cancellationToken)
    {
        var templateData = new GenerateInvestmentContractTemplateData
        {
            ContractDate = contractDate,
            LenderFullName = lenderFullName,
            FirstName = firstName,
            LastName = lastName,
            CompanyName = companyName,
            InvestmentAmount = investmentAmount,
            EquityPercentage = equityPercentage,
            BorrowerCompanyName = options.Value.BorrowerCompanyName,
            BorrowerCompanyAddress = options.Value.BorrowerCompanyAddress,
            BorrowerRegisterNumber = options.Value.BorrowerRegisterNumber
        };

        var command = new GenerateDocumentCommand(
            InvestmentContractTemplateFileName,
            InvestmentContractTemplateContent.Value,
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
        catch (Exception exception)
        {
            throw new DocumentProcessingException("PDF conversion failed.", exception);
        }
        finally
        {
            await response.Content.DisposeAsync();
        }
    }
}
