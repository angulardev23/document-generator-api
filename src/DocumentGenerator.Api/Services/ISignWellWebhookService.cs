using DocumentGenerator.Api.Contracts;

namespace DocumentGenerator.Api.Services;

public interface ISignWellWebhookService
{
    Task HandleAsync(
        SignWellWebhookRequest request,
        CancellationToken cancellationToken);
}
