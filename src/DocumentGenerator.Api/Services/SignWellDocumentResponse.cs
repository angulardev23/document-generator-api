namespace DocumentGenerator.Api.Services;

public sealed record SignWellDocumentResponse(
    string DocumentId,
    string SignWellUrl);
