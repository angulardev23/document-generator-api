using System.ComponentModel.DataAnnotations;

namespace DocumentGenerator.Application.Documents;

public sealed class DocumentGenerationOptions
{
    public const string SectionName = "DocumentGeneration";

    [Range(1, long.MaxValue)]
    public long MaxUploadFileSizeBytes { get; init; } = 5 * 1024 * 1024;

    [Required]
    public string OutputFilenamePrefix { get; init; } = "generated-document";
}

