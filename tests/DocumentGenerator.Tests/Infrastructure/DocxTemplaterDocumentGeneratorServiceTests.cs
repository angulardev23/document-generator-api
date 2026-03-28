using System.IO.Compression;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentGenerator.Infrastructure.Documents;

namespace DocumentGenerator.Tests.Infrastructure;

public sealed class DocxTemplaterDocumentGeneratorServiceTests
{
    [Fact]
    public async Task GenerateAsync_WithValidTemplate_ReplacesPlaceholder()
    {
        var service = new DocxTemplaterDocumentGeneratorService();
        using var templateStream = new MemoryStream(DocxTemplateFactory.CreateTemplate("{{title}}"));
        using var dataDocument = JsonDocument.Parse("""{"title":"Infrastructure Test"}""");

        var result = await service.GenerateAsync(
            templateStream,
            dataDocument.RootElement.Clone(),
            CancellationToken.None);

        using var resultStream = new MemoryStream();
        await result.Content.CopyToAsync(resultStream);

        Assert.Contains("Infrastructure Test", ExtractDocumentText(resultStream.ToArray()), StringComparison.Ordinal);
    }

    private static string ExtractDocumentText(byte[] generatedBytes)
    {
        using var memoryStream = new MemoryStream(generatedBytes);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
        using var documentStream = archive.GetEntry("word/document.xml")!.Open();
        using var reader = new StreamReader(documentStream, Encoding.UTF8);

        return reader.ReadToEnd();
    }

    private static class DocxTemplateFactory
    {
        public static byte[] CreateTemplate(string placeholderText)
        {
            using var memoryStream = new MemoryStream();

            using (var document = WordprocessingDocument.Create(
                       memoryStream,
                       WordprocessingDocumentType.Document,
                       autoSave: true))
            {
                var mainDocumentPart = document.AddMainDocumentPart();
                mainDocumentPart.Document = new Document(
                    new Body(
                        new Paragraph(
                            new Run(
                                new Text(placeholderText)))));
            }

            return memoryStream.ToArray();
        }
    }
}
