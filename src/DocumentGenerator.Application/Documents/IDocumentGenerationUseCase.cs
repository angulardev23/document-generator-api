namespace DocumentGenerator.Application.Documents;

public interface IDocumentGenerationUseCase
{
    Task<GeneratedDocumentResponse> GenerateAsync(
        GenerateDocumentCommand command,
        CancellationToken cancellationToken);
}

