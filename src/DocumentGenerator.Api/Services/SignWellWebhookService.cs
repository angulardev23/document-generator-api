using DocumentGenerator.Api.Contracts;
using DocumentGenerator.Domain.Documents;
using DocumentGenerator.Domain.InvestmentContracts;
using DocumentGenerator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace DocumentGenerator.Api.Services;

public sealed class SignWellWebhookService(
    ISignWellClient signWellClient,
    IInvestmentContractRepository investmentContractRepository,
    IStoredDocumentRepository storedDocumentRepository,
    DocumentGeneratorDbContext dbContext,
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
        if (!IsCompletedEvent(request.Event?.Type))
        {
            return;
        }

        var signWellDocumentId = request.Data?.Object?.Id;
        if (string.IsNullOrWhiteSpace(signWellDocumentId))
        {
            logger.LogWarning("Received SignWell completed webhook without a document id.");
            return;
        }

        await HandleCompletedDocumentAsync(
            signWellDocumentId,
            request.Data?.Object?.Name,
            cancellationToken);
    }

    private async Task HandleCompletedDocumentAsync(
        string signWellDocumentId,
        string? documentName,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.BeginSerializableTransactionIfRelationalAsync(cancellationToken);

        try
        {
            var investmentContract = await investmentContractRepository.GetBySignWellDocumentIdAsync(
                signWellDocumentId,
                cancellationToken);

            if (await TryCompleteEarlyAsync(investmentContract, signWellDocumentId, transaction, cancellationToken))
            {
                return;
            }

            await StoreSignedDocumentAsync(
                investmentContract!,
                signWellDocumentId,
                documentName,
                cancellationToken);

            await CommitAsync(transaction, cancellationToken);
        }
        catch (Exception exception) when (exception.IsSerializableConflict())
        {
            await RollbackAsync(transaction, cancellationToken);

            if (await WasHandledByConcurrentRequestAsync(signWellDocumentId, cancellationToken))
            {
                logger.LogInformation(
                    "Ignored duplicate SignWell completed webhook for document id {SignWellDocumentId} after serialization conflict.",
                    signWellDocumentId);
                return;
            }
            throw;
        }
    }

    private async Task<bool> TryCompleteEarlyAsync(
        InvestmentContract? investmentContract,
        string signWellDocumentId,
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (investmentContract is null)
        {
            logger.LogWarning(
                "Received SignWell completed webhook for unknown document id {SignWellDocumentId}.",
                signWellDocumentId);

            await CommitAsync(transaction, cancellationToken);
            return true;
        }

        if (IsSigned(investmentContract))
        {
            await CommitAsync(transaction, cancellationToken);
            return true;
        }

        return false;
    }

    private async Task StoreSignedDocumentAsync(
        InvestmentContract investmentContract,
        string signWellDocumentId,
        string? documentName,
        CancellationToken cancellationToken)
    {
        var signedDocument = await signWellClient.DownloadCompletedDocumentAsync(
            signWellDocumentId,
            cancellationToken);

        var storedDocument = new StoredDocument
        {
            FileName = ResolveFileName(signedDocument.FileName, documentName, signWellDocumentId),
            ContentType = signedDocument.ContentType,
            Content = signedDocument.Content
        };

        storedDocumentRepository.Add(storedDocument);

        investmentContract.SignedDocument = storedDocument;
        investmentContract.Status = InvestmentContract.SignedStatus;
        investmentContract.SignedAt = DateTime.UtcNow;

        await investmentContractRepository.SaveChangesAsync(cancellationToken);
    }

    private static async Task CommitAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is null)
        {
            return;
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task RollbackAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is null)
        {
            return;
        }

        await transaction.RollbackAsync(cancellationToken);
    }

    private async Task<bool> WasHandledByConcurrentRequestAsync(
        string signWellDocumentId,
        CancellationToken cancellationToken)
    {
        var investmentContract = await investmentContractRepository.GetBySignWellDocumentIdAsync(
            signWellDocumentId,
            cancellationToken);

        return investmentContract is not null && IsSigned(investmentContract);
    }

    private static bool IsCompletedEvent(string? eventType)
    {
        return CompletedEventTypes.Contains(eventType ?? string.Empty);
    }

    private static bool IsSigned(InvestmentContract investmentContract)
    {
        return investmentContract is { Status: InvestmentContract.SignedStatus, SignedDocumentId: not null };
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
