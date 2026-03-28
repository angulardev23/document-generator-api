namespace DocumentGenerator.Application.Documents;

public sealed record GeneratedDocumentResponse(
    Stream Content,
    string ContentType,
    string FileName);

