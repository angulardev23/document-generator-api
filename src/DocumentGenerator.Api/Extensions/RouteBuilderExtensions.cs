using DocumentGenerator.Api.Routes;

namespace DocumentGenerator.Api.Extensions;

public static class RouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapRouteModules(this IEndpointRouteBuilder endpoints)
    {
        var routeModuleTypes = typeof(IRouteModule).Assembly
            .GetTypes()
            .Where(type => typeof(IRouteModule).IsAssignableFrom(type))
            .Where(type => type is { IsInterface: false, IsAbstract: false })
            .OrderBy(type => type.Name);

        foreach (var routeModuleType in routeModuleTypes)
        {
            var routeModule = (IRouteModule)Activator.CreateInstance(routeModuleType)!;
            routeModule.MapRoutes(endpoints);
        }

        return endpoints;
    }
}
