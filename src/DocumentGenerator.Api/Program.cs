using DocumentGenerator.Api.Configuration;
using DocumentGenerator.Api.Extensions;
using DocumentGenerator.Api.ExceptionHandling;
using DocumentGenerator.Application;
using DocumentGenerator.Application.Documents;
using DocumentGenerator.Infrastructure;

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

app.MapEndpoints();

app.Run();

public partial class Program;
