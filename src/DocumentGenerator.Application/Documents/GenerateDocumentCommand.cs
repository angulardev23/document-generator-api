namespace DocumentGenerator.Application.Documents;

public sealed record GenerateDocumentCommand(
    string? TemplateFileName,
    byte[]? TemplateContent,
    string? Data);

