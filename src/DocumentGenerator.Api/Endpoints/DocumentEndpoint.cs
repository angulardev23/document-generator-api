using System.Text.Json;
using DocumentGenerator.Api.Contracts;
using DocumentGenerator.Api.Services;
using DocumentGenerator.Application.Documents;
using Microsoft.AspNetCore.Mvc;

namespace DocumentGenerator.Api.Endpoints;

public sealed class DocumentEndpoint : IEndpoint
{
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
            .WithSummary("Generates the default investment contract PDF from a JSON payload.")
            .Accepts<GenerateInvestmentContractRequest>("application/json")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

        documentsGroup.MapPost(
                "/investment-contract/signwell",
                GenerateInvestmentContractSignWellAsync)
            .WithName("GenerateInvestmentContractSignWell")
            .WithSummary("Generates the default investment contract PDF, uploads it to SignWell, and returns the SignWell URL.")
            .Accepts<GenerateInvestmentContractSignWellRequest>("application/json")
            .Produces<GenerateInvestmentContractSignWellResponse>(StatusCodes.Status200OK, contentType: "application/json")
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
        IInvestmentContractDocumentService investmentContractDocumentService,
        CancellationToken cancellationToken)
    {
        GeneratedDocumentResponse response = await investmentContractDocumentService.GenerateAsync(request, cancellationToken);

        return CreateFileResult(response);
    }

    private static async Task<IResult> GenerateInvestmentContractSignWellAsync(
        [FromBody] GenerateInvestmentContractSignWellRequest request,
        IInvestmentContractDocumentService investmentContractDocumentService,
        CancellationToken cancellationToken)
    {
        var response = await investmentContractDocumentService.GenerateAndUploadToSignWellAsync(request, cancellationToken);

        return Results.Ok(response);
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
