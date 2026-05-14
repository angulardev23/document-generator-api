namespace DocumentGenerator.Api.Routes;

public sealed class HealthRoutes : IRouteModule
{
    public void MapRoutes(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("HealthCheck")
            .WithSummary("Returns the application health status.")
            .Produces(StatusCodes.Status200OK);
    }
}
