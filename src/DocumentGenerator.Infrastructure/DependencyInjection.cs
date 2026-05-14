using DocumentGenerator.Domain.Documents;
using DocumentGenerator.Domain.Services;
using DocumentGenerator.Infrastructure.Documents;
using DocumentGenerator.Infrastructure.Persistence;
using DocumentGenerator.Domain.InvestmentContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentGenerator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<DocumentGeneratorDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(DocumentGeneratorDbContext).Assembly.FullName)));

        services.AddScoped<IStoredDocumentRepository, StoredDocumentRepository>();
        services.AddScoped<IInvestmentContractRepository, InvestmentContractRepository>();
        services.AddScoped<IDocumentGeneratorService, DocxTemplaterDocumentGeneratorService>();

        if (!OperatingSystem.IsMacOS())
        {
            services.AddHostedService<LibreOfficeHostedService>();
        }

        services.AddScoped<IWordToPdfConverterService, LibreOfficeWordToPdfConverterService>();

        return services;
    }
}
