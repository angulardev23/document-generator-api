using DocumentGenerator.Domain.Documents;

namespace DocumentGenerator.Infrastructure.Persistence;

public sealed class StoredDocumentRepository(DocumentGeneratorDbContext dbContext) : IStoredDocumentRepository
{
    public void Add(StoredDocument document)
    {
        dbContext.Documents.Add(document);
    }
}
