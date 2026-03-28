using DocumentGenerator.Api.Configuration;
using DocumentGenerator.Api.Contracts;
using DocumentGenerator.Api.ExceptionHandling;
using DocumentGenerator.Application;
using DocumentGenerator.Application.Documents;
using DocumentGenerator.Infrastructure;
using Microsoft.AspNetCore.Mvc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<DocumentGenerationOptions>()
    .Bind(builder.Configuration.GetSection(DocumentGenerationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<
    Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Http.Features.FormOptions>,
    ConfigureMultipartFormOptions>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

await using WebApplication app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var documentsGroup = app.MapGroup("/api/documents");

documentsGroup.MapPost(
        "/generate",
        async (
            [FromForm] GenerateDocumentRequest request,
            IDocumentGenerationUseCase useCase,
            CancellationToken cancellationToken) =>
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
        })
    .WithName("GenerateDocument")
    .WithSummary("Generates a DOCX document from a DOCX template and JSON payload.")
    .Accepts<GenerateDocumentRequest>("multipart/form-data")
    .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
    .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
    .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
    .DisableAntiforgery();

app.Run();

public partial class Program;

