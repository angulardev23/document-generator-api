namespace DocumentGenerator.Api.Configuration;

public sealed class SignWellOptions
{
    public const string SectionName = "SignWell";

    public string BaseUrl { get; init; } = "https://www.signwell.com";

    public string ApiKey { get; init; } = string.Empty;

    public bool TestMode { get; init; } = true;
}
