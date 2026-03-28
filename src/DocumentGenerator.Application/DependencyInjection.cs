using DocumentGenerator.Application.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentGenerator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDocumentGenerationUseCase, DocumentGenerationUseCase>();
        services.AddScoped<GenerateDocumentCommandValidator>();

        return services;
    }
}

