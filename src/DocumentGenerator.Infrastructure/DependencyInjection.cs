using DocumentGenerator.Domain.Services;
using DocumentGenerator.Infrastructure.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentGenerator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDocumentGeneratorService, DocxTemplaterDocumentGeneratorService>();

        return services;
    }
}

