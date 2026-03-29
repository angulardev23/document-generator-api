using DocumentGenerator.Api.Endpoints;

namespace DocumentGenerator.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var endpointTypes = typeof(IEndpoint).Assembly
            .GetTypes()
            .Where(type => typeof(IEndpoint).IsAssignableFrom(type))
            .Where(type => type is { IsInterface: false, IsAbstract: false })
            .OrderBy(type => type.Name);

        foreach (var endpointType in endpointTypes)
        {
            var endpoint = (IEndpoint)Activator.CreateInstance(endpointType)!;
            endpoint.MapEndpoint(endpoints);
        }

        return endpoints;
    }
}
