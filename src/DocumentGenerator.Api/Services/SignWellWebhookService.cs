using DocumentGenerator.Api.Contracts;
using DocumentGenerator.Domain.Documents;
using DocumentGenerator.Domain.InvestmentContracts;

namespace DocumentGenerator.Api.Services;

public sealed class SignWellWebhookService(
    ISignWellClient signWellClient,
    IInvestmentContractRepository investmentContractRepository,
    IStoredDocumentRepository storedDocumentRepository,
    ILogger<SignWellWebhookService> logger) : ISignWellWebhookService
{
    private static readonly HashSet<string> CompletedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "document_completed",
        "document_signed"
    };

    public async Task HandleAsync(
        SignWellWebhookRequest request,
        CancellationToken cancellationToken)
    {
        if (!CompletedEventTypes.Contains(request.Event?.Type ?? string.Empty))
        {
            return;
        }

        var signWellDocumentId = request.Data?.Object?.Id;
        if (string.IsNullOrWhiteSpace(signWellDocumentId))
        {
            logger.LogWarning("Received SignWell completed webhook without a document id.");
            return;
        }

        var investmentContract = await investmentContractRepository.GetBySignWellDocumentIdAsync(
            signWellDocumentId,
            cancellationToken);

        if (investmentContract is null)
        {
            logger.LogWarning(
                "Received SignWell completed webhook for unknown document id {SignWellDocumentId}.",
                signWellDocumentId);
            return;
        }

        if (investmentContract is { Status: InvestmentContract.SignedStatus, SignedDocumentId: not null })
        {
            return;
        }

        var signedDocument = await signWellClient.DownloadCompletedDocumentAsync(
            signWellDocumentId,
            cancellationToken);

        var storedDocument = new StoredDocument
        {
            FileName = ResolveFileName(signedDocument.FileName, request.Data?.Object?.Name, signWellDocumentId),
            ContentType = signedDocument.ContentType,
            Content = signedDocument.Content
        };

        storedDocumentRepository.Add(storedDocument);

        investmentContract.SignedDocument = storedDocument;
        investmentContract.Status = InvestmentContract.SignedStatus;
        investmentContract.SignedAt = DateTime.UtcNow;

        await investmentContractRepository.SaveChangesAsync(cancellationToken);
    }

    private static string ResolveFileName(
        string? downloadedFileName,
        string? documentName,
        string signWellDocumentId)
    {
        if (!string.IsNullOrWhiteSpace(downloadedFileName))
        {
            return downloadedFileName;
        }

        if (!string.IsNullOrWhiteSpace(documentName))
        {
            return documentName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? documentName
                : $"{documentName}.pdf";
        }

        return $"signwell-{signWellDocumentId}.pdf";
    }
}
