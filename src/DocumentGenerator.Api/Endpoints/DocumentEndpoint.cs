using System.Text.Json;
using DocumentGenerator.Api.Contracts;
using DocumentGenerator.Application.Documents;
using Microsoft.AspNetCore.Mvc;

namespace DocumentGenerator.Api.Endpoints;

public sealed class DocumentEndpoint : IEndpoint
{
    private const string InvestmentContractTemplateFileName = "InvestmentContract.docx";
    private static readonly string InvestmentContractTemplatePath = Path.Combine(
        AppContext.BaseDirectory,
        "templates",
        InvestmentContractTemplateFileName);

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var documentsGroup = endpoints.MapGroup("/api/documents");

        documentsGroup.MapPost(
                "/generate",
                GenerateAsync)
            .WithName("GenerateDocument")
            .WithSummary("Generates a DOCX document from a DOCX template and JSON payload.")
            .Accepts<GenerateDocumentRequest>("multipart/form-data")
            .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

        documentsGroup.MapPost(
                "/investment-contract",
                GenerateInvestmentContractAsync)
            .WithName("GenerateInvestmentContract")
            .WithSummary("Generates the default investment contract DOCX from a JSON payload.")
            .Accepts<GenerateInvestmentContractRequest>("application/json")
            .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();
    }

    private static async Task<IResult> GenerateAsync(
        [FromForm] GenerateDocumentRequest request,
        IDocumentGenerationUseCase useCase,
        CancellationToken cancellationToken)
    {
        byte[]? templateContent = null;

        if (request.Template is not null)
        {
            await using var memoryStream = new MemoryStream();
            await request.Template.CopyToAsync(memoryStream, cancellationToken);
            templateContent = memoryStream.ToArray();
        }

        var command = new GenerateDocumentCommand(
            request.Template?.FileName,
            templateContent,
            request.Data);

        GeneratedDocumentResponse response = await useCase.GenerateAsync(command, cancellationToken);

        return CreateFileResult(response);
    }

    private static async Task<IResult> GenerateInvestmentContractAsync(
        [FromBody] GenerateInvestmentContractRequest request,
        IDocumentGenerationUseCase useCase,
        CancellationToken cancellationToken)
    {
        var templateContent = await File.ReadAllBytesAsync(InvestmentContractTemplatePath, cancellationToken);

        var command = new GenerateDocumentCommand(
            InvestmentContractTemplateFileName,
            templateContent,
            JsonSerializer.Serialize(request));

        GeneratedDocumentResponse response = await useCase.GenerateAsync(command, cancellationToken);

        return CreateFileResult(response);
    }

    private static IResult CreateFileResult(GeneratedDocumentResponse response)
    {
        response.Content.Position = 0;

        return Results.File(
            response.Content,
            response.ContentType,
            response.FileName);
    }
}
