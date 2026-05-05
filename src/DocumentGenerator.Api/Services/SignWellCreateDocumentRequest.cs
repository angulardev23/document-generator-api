namespace DocumentGenerator.Api.Services;

public sealed record SignWellCreateDocumentRequest(
    string FileName,
    Stream Content,
    string RecipientName,
    string RecipientEmail,
    string? RedirectUrl);
