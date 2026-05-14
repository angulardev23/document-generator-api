using DocumentGenerator.Api.Contracts;
using DocumentGenerator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentGenerator.Api.Endpoints;

public sealed class WebhooksEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/webhooks/signwell",
                HandleSignWellAsync)
            .WithName("HandleSignWellWebhook")
            .WithSummary("Receives SignWell webhook events and stores completed signed PDFs.")
            .Accepts<SignWellWebhookRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .DisableAntiforgery();
    }

    private static async Task<IResult> HandleSignWellAsync(
        [FromBody] SignWellWebhookRequest request,
        ISignWellWebhookService signWellWebhookService,
        CancellationToken cancellationToken)
    {
        await signWellWebhookService.HandleAsync(request, cancellationToken);

        return Results.Ok();
    }
}
