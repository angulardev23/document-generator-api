using DocumentGenerator.Domain.Documents;

namespace DocumentGenerator.Domain.InvestmentContracts;

public sealed class InvestmentContract : IAuditable
{
    public const string PendingSignatureStatus = "pending_signature";
    public const string SignedStatus = "signed";

    public Guid Id { get; set; } = Guid.NewGuid();

    public required int ListingId { get; set; }

    public required int UserId { get; set; }

    public required string SignWellDocumentId { get; set; }

    public required string Status { get; set; }

    public Guid? SignedDocumentId { get; set; }

    public StoredDocument? SignedDocument { get; set; }

    public DateTime? SignedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
