using System.Text.Json.Serialization;

namespace DocumentGenerator.Api.Contracts;

public sealed class SignWellWebhookRequest
{
    [JsonPropertyName("event")]
    public SignWellWebhookEvent? Event { get; init; }

    [JsonPropertyName("data")]
    public SignWellWebhookData? Data { get; init; }
}

public sealed class SignWellWebhookEvent
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }
}

public sealed class SignWellWebhookData
{
    [JsonPropertyName("object")]
    public SignWellWebhookDocument? Object { get; init; }
}

public sealed class SignWellWebhookDocument
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
