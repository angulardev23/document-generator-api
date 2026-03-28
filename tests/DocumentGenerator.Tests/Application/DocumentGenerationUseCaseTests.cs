using System.Text;
using System.Text.Json;
using DocumentGenerator.Application.Documents;
using DocumentGenerator.Application.Exceptions;
using DocumentGenerator.Domain.Documents;
using DocumentGenerator.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocumentGenerator.Tests.Application;

public sealed class DocumentGenerationUseCaseTests
{
    [Fact]
    public async Task GenerateAsync_WithValidRequest_ReturnsGeneratedDocument()
    {
        var fakeGenerator = new FakeDocumentGeneratorService();
        var useCase = CreateUseCase(fakeGenerator);
        var command = new GenerateDocumentCommand(
            "template.docx",
            Encoding.UTF8.GetBytes("template"),
            """{"title":"Agreement","customer":{"name":"Jane Doe"}}""");

        var response = await useCase.GenerateAsync(command, CancellationToken.None);

        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", response.ContentType);
        Assert.StartsWith("generated-document-", response.FileName, StringComparison.Ordinal);
        Assert.Equal("Agreement", fakeGenerator.LastData.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GenerateAsync_WithInvalidJson_ThrowsValidationException()
    {
        var useCase = CreateUseCase(new FakeDocumentGeneratorService());
        var command = new GenerateDocumentCommand(
            "template.docx",
            Encoding.UTF8.GetBytes("template"),
            "{invalid");

        var exception = await Assert.ThrowsAsync<ValidationException>(() => useCase.GenerateAsync(command, CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.Field == "data");
    }

    [Fact]
    public async Task GenerateAsync_WithJsonArray_ThrowsValidationException()
    {
        var useCase = CreateUseCase(new FakeDocumentGeneratorService());
        var command = new GenerateDocumentCommand(
            "template.docx",
            Encoding.UTF8.GetBytes("template"),
            """["not","an","object"]""");

        var exception = await Assert.ThrowsAsync<ValidationException>(() => useCase.GenerateAsync(command, CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.Message.Contains("JSON object", StringComparison.Ordinal));
    }

    private static DocumentGenerationUseCase CreateUseCase(IDocumentGeneratorService generatorService)
    {
        var options = Options.Create(new DocumentGenerationOptions
        {
            MaxUploadFileSizeBytes = 1024 * 1024,
            OutputFilenamePrefix = "generated-document"
        });

        return new DocumentGenerationUseCase(
            generatorService,
            new GenerateDocumentCommandValidator(options),
            options,
            NullLogger<DocumentGenerationUseCase>.Instance);
    }

    private sealed class FakeDocumentGeneratorService : IDocumentGeneratorService
    {
        public JsonElement LastData { get; private set; }

        public Task<GeneratedDocument> GenerateAsync(
            Stream templateStream,
            JsonElement data,
            CancellationToken cancellationToken)
        {
            LastData = data.Clone();

            Stream content = new MemoryStream(Encoding.UTF8.GetBytes("generated"));
            return Task.FromResult(new GeneratedDocument(
                content,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
        }
    }
}
