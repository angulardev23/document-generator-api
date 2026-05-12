namespace DocumentGenerator.Domain.InvestmentContracts;

public sealed class InvestmentContract
{
    public const string PendingSignatureStatus = "pending_signature";

    public Guid Id { get; set; } = Guid.NewGuid();

    public required string ListingId { get; set; }

    public required string UserId { get; set; }

    public required string SignWellDocumentId { get; set; }

    public required string Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
