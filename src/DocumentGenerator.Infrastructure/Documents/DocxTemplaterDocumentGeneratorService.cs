using System.Text.Json;
using DocxTemplater;
using DocumentGenerator.Domain.Documents;
using DocumentGenerator.Domain.Services;

namespace DocumentGenerator.Infrastructure.Documents;

public sealed class DocxTemplaterDocumentGeneratorService : IDocumentGeneratorService
{
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public Task<GeneratedDocument> GenerateAsync(
        Stream templateStream,
        JsonElement data,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(templateStream);

        cancellationToken.ThrowIfCancellationRequested();

        if (templateStream.CanSeek)
        {
            templateStream.Position = 0;
        }

        var model = JsonTemplateModelConverter.Convert(data);
        var template = new DocxTemplate(templateStream);

        foreach (var property in model)
        {
            template.BindModel(property.Key, property.Value);
        }

        var resultStream = template.Process();
        resultStream.Position = 0;

        var outputStream = new MemoryStream();
        resultStream.CopyTo(outputStream);
        outputStream.Position = 0;

        return Task.FromResult(new GeneratedDocument(outputStream, DocxContentType));
    }
}
