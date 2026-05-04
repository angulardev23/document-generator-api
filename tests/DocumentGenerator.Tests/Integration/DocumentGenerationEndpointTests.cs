using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DocumentGenerator.Tests.Integration;

public sealed class DocumentGenerationEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory.WithWebHostBuilder(_ => { });

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health");
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", payload.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PostGenerate_WithValidMultipartRequest_ReturnsGeneratedDocx()
    {
        using var client = _factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var templateContent = new ByteArrayContent(DocxTemplateFactory.CreateTemplate("{{title}}"));

        templateContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        form.Add(templateContent, "template", "template.docx");
        form.Add(new StringContent("""{"title":"Generated Contract"}""", Encoding.UTF8, "application/json"), "data");

        using var response = await client.PostAsync("/api/documents/generate", form);
        var generatedBytes = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(
            "attachment",
            response.Content.Headers.ContentDisposition?.DispositionType ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Generated Contract", ExtractDocumentText(generatedBytes), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostGenerate_WithWrongFileType_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var templateContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not-a-docx"));

        templateContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        form.Add(templateContent, "template", "template.txt");
        form.Add(new StringContent("""{"title":"Ignored"}""", Encoding.UTF8, "application/json"), "data");

        using var response = await client.PostAsync("/api/documents/generate", form);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", payload.RootElement.GetProperty("code").GetString());
        Assert.Contains(
            payload.RootElement.GetProperty("errors").EnumerateArray().Select(error => error.GetProperty("field").GetString()),
            field => field == "template");
    }

    [Fact]
    public async Task PostGenerate_WithMissingPlaceholderData_ReturnsBadRequestWithPlaceholderDetails()
    {
        using var client = _factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var templateContent = new ByteArrayContent(DocxTemplateFactory.CreateTemplate("{{FirstName}}"));

        templateContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        form.Add(templateContent, "template", "template.docx");
        form.Add(new StringContent("""{"LastName":"Escalante"}""", Encoding.UTF8, "application/json"), "data");

        using var response = await client.PostAsync("/api/documents/generate", form);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = payload.RootElement.GetProperty("errors").EnumerateArray().ToArray();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", payload.RootElement.GetProperty("code").GetString());
        Assert.Contains(
            errors,
            error => error.GetProperty("field").GetString() == "data.FirstName" &&
                     error.GetProperty("message").GetString()!.Contains("{{FirstName}}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PostInvestmentContract_WithValidJsonBody_ReturnsGeneratedDocx()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsync(
            "/api/documents/investment-contract",
            new StringContent(
                """
                {
                  "ContractDate": "2026-03-30",
                  "LenderFullName": "Carlitos Escalante",
                  "FirstName": "Carlitos",
                  "LastName": "Escalante",
                  "CompanyName": "Example Ventures",
                  "InvestmentAmount": "100000 USD",
                  "EquityPercentage": "10%"
                }
                """,
                Encoding.UTF8,
                "application/json"));

        var generatedBytes = await response.Content.ReadAsByteArrayAsync();
        var documentText = ExtractDocumentText(generatedBytes);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(
            "attachment",
            response.Content.Headers.ContentDisposition?.DispositionType ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Venturice GmbH", documentText, StringComparison.Ordinal);
        Assert.Contains("Wolframstraße 24, 70191 Stuttgart, Germany", documentText, StringComparison.Ordinal);
        Assert.Contains("HRB 123456 B", documentText, StringComparison.Ordinal);
        Assert.Contains("2026-03-30", documentText, StringComparison.Ordinal);
        Assert.Contains("Carlitos Escalante", documentText, StringComparison.Ordinal);
        Assert.Contains("Carlitos", documentText, StringComparison.Ordinal);
        Assert.Contains("Escalante", documentText, StringComparison.Ordinal);
        Assert.Contains("Example Ventures", documentText, StringComparison.Ordinal);
        Assert.Contains("100000 USD", documentText, StringComparison.Ordinal);
        Assert.Contains("10", documentText, StringComparison.Ordinal);
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
