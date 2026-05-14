using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentGenerator.Api.Configuration;
using Microsoft.Extensions.Options;

namespace DocumentGenerator.Api.Services;

public sealed class SignWellClient(
    HttpClient httpClient,
    IOptions<SignWellOptions> options) : ISignWellClient
{
    private const string SignWellApiKeyHeaderName = "X-Api-Key";
    private readonly SignWellOptions _options = options.Value;

    public async Task<SignWellDocumentResponse> CreateDocumentAsync(
        SignWellCreateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var contentStream = new MemoryStream();
        request.Content.Position = 0;
        await request.Content.CopyToAsync(contentStream, cancellationToken);

        var payload = new CreateDocumentPayload
        {
            TestMode = _options.TestMode,
            Name = Path.GetFileNameWithoutExtension(request.FileName),
            EmbeddedSigning = true,
            WithSignaturePage = true,
            RedirectUrl = request.RedirectUrl,
            Files =
            [
                new CreateDocumentFilePayload
                {
                    Name = request.FileName,
                    FileBase64 = Convert.ToBase64String(contentStream.ToArray())
                }
            ],
            Recipients =
            [
                new CreateDocumentRecipientPayload
                {
                    Id = "1",
                    Name = request.RecipientName,
                    Email = request.RecipientEmail
                }
            ]
        };

        using var httpRequest = CreateRequest(HttpMethod.Post, BuildDocumentsUri());
        httpRequest.Content = JsonContent.Create(
            payload,
            mediaType: new MediaTypeHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"SignWell document creation failed with status {(int)response.StatusCode}: {body}");
        }

        using var responseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));
        var createdDocument = await JsonSerializer.DeserializeAsync<CreateDocumentResponse>(
            responseStream,
            cancellationToken: cancellationToken);

        if (createdDocument?.Id is null)
        {
            throw new InvalidOperationException("SignWell document creation succeeded but no document id was returned.");
        }

        var signWellUrl = createdDocument.Recipients?
            .FirstOrDefault()?
            .EmbeddedSigningUrl;

        if (string.IsNullOrWhiteSpace(signWellUrl))
        {
            throw new InvalidOperationException(
                "SignWell document creation succeeded but no embedded signing URL was returned.");
        }

        return new SignWellDocumentResponse(createdDocument.Id, signWellUrl);
    }

    public async Task<SignWellCompletedDocumentResponse> DownloadCompletedDocumentAsync(
        string documentId,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var httpRequest = CreateRequest(
            HttpMethod.Get,
            BuildCompletedPdfUri(documentId));

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = System.Text.Encoding.UTF8.GetString(content);
            throw new InvalidOperationException(
                $"SignWell completed document download failed with status {(int)response.StatusCode}: {body}");
        }

        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName;

        return new SignWellCompletedDocumentResponse(
            SanitizeFileName(fileName),
            response.Content.Headers.ContentType?.MediaType ?? "application/pdf",
            content);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("SignWell API key is not configured.");
        }

        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("SignWell base URL is not configured correctly.");
        }
    }

    private Uri BuildDocumentsUri()
    {
        return new Uri(new Uri(_options.BaseUrl, UriKind.Absolute), "/api/v1/documents");
    }

    private Uri BuildCompletedPdfUri(string documentId)
    {
        return new Uri(
            new Uri(_options.BaseUrl, UriKind.Absolute),
            $"/api/v1/documents/{Uri.EscapeDataString(documentId)}/completed_pdf?url_only=false");
    }

    private HttpRequestMessage CreateRequest(HttpMethod httpMethod, Uri uri)
    {
        var request = new HttpRequestMessage(httpMethod, uri);
        request.Headers.Add(SignWellApiKeyHeaderName, _options.ApiKey);
        return request;
    }

    private static string? SanitizeFileName(string? fileName)
    {
        return string.IsNullOrWhiteSpace(fileName)
            ? null
            : fileName.Trim('"');
    }

    private sealed class CreateDocumentPayload
    {
        [JsonPropertyName("test_mode")]
        public bool TestMode { get; init; }

        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("embedded_signing")]
        public bool EmbeddedSigning { get; init; }

        [JsonPropertyName("with_signature_page")]
        public bool WithSignaturePage { get; init; }

        [JsonPropertyName("redirect_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RedirectUrl { get; init; }

        [JsonPropertyName("files")]
        public required IReadOnlyList<CreateDocumentFilePayload> Files { get; init; }

        [JsonPropertyName("recipients")]
        public required IReadOnlyList<CreateDocumentRecipientPayload> Recipients { get; init; }
    }

    private sealed class CreateDocumentFilePayload
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("file_base64")]
        public required string FileBase64 { get; init; }
    }

    private sealed class CreateDocumentRecipientPayload
    {
        [JsonPropertyName("id")]
        public required string Id { get; init; }

        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("email")]
        public required string Email { get; init; }
    }

    private sealed class CreateDocumentResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("recipients")]
        public IReadOnlyList<CreateDocumentRecipientResponse>? Recipients { get; init; }
    }

    private sealed class CreateDocumentRecipientResponse
    {
        [JsonPropertyName("embedded_signing_url")]
        public string? EmbeddedSigningUrl { get; init; }
    }
}
