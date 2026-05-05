using DocumentGenerator.Domain.Documents;

namespace DocumentGenerator.Domain.Services;

public interface IWordToPdfConverterService
{
    Task<GeneratedDocument> ConvertAsync(
        Stream wordDocumentStream,
        CancellationToken cancellationToken);
}
