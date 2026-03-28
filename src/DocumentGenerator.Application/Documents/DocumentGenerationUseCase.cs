using DocumentGenerator.Application.Exceptions;
using DocumentGenerator.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentGenerator.Application.Documents;

public sealed class DocumentGenerationUseCase(
    IDocumentGeneratorService documentGeneratorService,
    GenerateDocumentCommandValidator validator,
    IOptions<DocumentGenerationOptions> options,
    ILogger<DocumentGenerationUseCase> logger) : IDocumentGenerationUseCase
{
    public async Task<GeneratedDocumentResponse> GenerateAsync(
        GenerateDocumentCommand command,
        CancellationToken cancellationToken)
    {
        var validatedRequest = validator.Validate(command);

        await using var templateStream = new MemoryStream(validatedRequest.TemplateContent, writable: false);

        try
        {
            var generatedDocument = await documentGeneratorService.GenerateAsync(
                templateStream,
                validatedRequest.DataRoot,
                cancellationToken);

            generatedDocument.Content.Position = 0;

            var fileName = BuildOutputFileName(options.Value.OutputFilenamePrefix);

            logger.LogInformation(
                "Generated document for template {TemplateFileName} with output {OutputFileName}.",
                validatedRequest.TemplateFileName,
                fileName);

            return new GeneratedDocumentResponse(
                generatedDocument.Content,
                generatedDocument.ContentType,
                fileName);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (TemplateProcessingErrorParser.TryParse(exception, out var validationErrors))
            {
                throw new ValidationException(validationErrors);
            }

            logger.LogError(
                exception,
                "Document generation failed for template {TemplateFileName}.",
                validatedRequest.TemplateFileName);

            throw new DocumentProcessingException("Document generation failed.", exception);
        }
    }

    private static string BuildOutputFileName(string configuredPrefix)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitizedPrefix = new string(configuredPrefix.Where(character => !invalidCharacters.Contains(character)).ToArray()).Trim();

        if (string.IsNullOrWhiteSpace(sanitizedPrefix))
        {
            sanitizedPrefix = "document";
        }

        return $"{sanitizedPrefix}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.docx";
    }
}
