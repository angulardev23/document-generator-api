namespace DocumentGenerator.Domain.Documents;

public sealed class StoredDocument : IAuditable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string FileName { get; set; }

    public required string ContentType { get; set; }

    public required byte[] Content { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
