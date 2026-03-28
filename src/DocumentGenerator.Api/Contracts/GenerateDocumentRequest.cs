using Microsoft.AspNetCore.Mvc;

namespace DocumentGenerator.Api.Contracts;

public sealed class GenerateDocumentRequest
{
    [FromForm(Name = "template")]
    public IFormFile? Template { get; init; }

    [FromForm(Name = "data")]
    public string? Data { get; init; }
}

