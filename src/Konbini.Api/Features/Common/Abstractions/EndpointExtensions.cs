namespace Konbini.Api.Features.Common.Abstractions;

/// <summary>
/// endpoint / handler 的組件掃描註冊。
/// 手寫輕量管線（約 40 行）取代 MediatR：加新用例＝加檔案，Program.cs 不動。
/// </summary>
public static class EndpointExtensions
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
    ];

    /// <summary>將組件內所有 Command/Query handler 以其介面註冊進 DI（Scoped）。</summary>
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        var types = typeof(EndpointExtensions).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && HandlerInterfaces.Contains(i.GetGenericTypeDefinition()));
            foreach (var itf in interfaces)
            {
                services.AddScoped(itf, type);
            }
        }
        return services;
    }

    /// <summary>Map 組件內所有 <see cref="IEndpoint"/>。</summary>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointTypes = typeof(EndpointExtensions).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IEndpoint).IsAssignableFrom(t));

        foreach (var type in endpointTypes)
        {
            var endpoint = (IEndpoint)Activator.CreateInstance(type)!;
            endpoint.Map(app);
        }
        return app;
    }
}
