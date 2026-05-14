using DocumentGenerator.Api.Services;

namespace DocumentGenerator.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddScoped<IInvestmentContractDocumentService, InvestmentContractDocumentService>();
        services.AddScoped<ISignWellWebhookService, SignWellWebhookService>();
        services.AddHttpClient<ISignWellClient, SignWellClient>();

        return services;
    }
}
