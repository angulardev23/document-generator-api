namespace DocumentGenerator.Domain.Documents;

public interface IStoredDocumentRepository
{
    void Add(StoredDocument document);
}
