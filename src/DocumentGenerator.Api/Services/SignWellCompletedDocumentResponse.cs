namespace DocumentGenerator.Api.Services;

public sealed record SignWellCompletedDocumentResponse(
    string? FileName,
    string ContentType,
    byte[] Content);
