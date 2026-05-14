using DocumentGenerator.Api;
using DocumentGenerator.Api.Configuration;
using DocumentGenerator.Api.Extensions;
using DocumentGenerator.Api.ExceptionHandling;
using DocumentGenerator.Application;
using DocumentGenerator.Application.Documents;
using DocumentGenerator.Infrastructure;
using DocumentGenerator.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<DocumentGenerationOptions>()
    .Bind(builder.Configuration.GetSection(DocumentGenerationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<InvestmentContractOptions>()
    .Bind(builder.Configuration.GetSection(InvestmentContractOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<SignWellOptions>()
    .Bind(builder.Configuration.GetSection(SignWellOptions.SectionName));

builder.Services.AddSingleton<IConfigureOptions<FormOptions>, ConfigureMultipartFormOptions>();
builder.Services.AddApiServices();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

await using WebApplication app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.MapRouteModules();

app.Run();

public partial class Program;
