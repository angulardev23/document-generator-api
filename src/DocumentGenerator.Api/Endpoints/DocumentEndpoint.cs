using DocumentGenerator.Api.Contracts;
using DocumentGenerator.Application.Documents;
using Microsoft.AspNetCore.Mvc;

namespace DocumentGenerator.Api.Endpoints;

public sealed class DocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
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

        response.Content.Position = 0;

        return Results.File(
            response.Content,
            response.ContentType,
            response.FileName);
    }
}
