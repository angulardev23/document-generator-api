namespace DocumentGenerator.Api.Endpoints;

public sealed class HealthEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("HealthCheck")
            .WithSummary("Returns the application health status.")
            .Produces(StatusCodes.Status200OK);
    }
}
