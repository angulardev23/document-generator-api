namespace DocumentGenerator.Api.Services;

public interface ISignWellClient
{
    Task<SignWellDocumentResponse> CreateDocumentAsync(
        SignWellCreateDocumentRequest request,
        CancellationToken cancellationToken);

    Task<SignWellCompletedDocumentResponse> DownloadCompletedDocumentAsync(
        string documentId,
        CancellationToken cancellationToken);
}
