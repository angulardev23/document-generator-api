using System.Text.Json;

namespace DocumentGenerator.Application.Documents;

public sealed record ValidatedGenerateDocumentCommand(
    string TemplateFileName,
    byte[] TemplateContent,
    JsonElement DataRoot);

