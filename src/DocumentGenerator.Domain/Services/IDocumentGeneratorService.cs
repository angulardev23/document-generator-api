using System.Text.Json;
using DocumentGenerator.Domain.Documents;

namespace DocumentGenerator.Domain.Services;

public interface IDocumentGeneratorService
{
    Task<GeneratedDocument> GenerateAsync(
        Stream templateStream,
        JsonElement data,
        CancellationToken cancellationToken);
}
