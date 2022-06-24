using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Configuration;
using DispatchEndpoints.Filters;

namespace DispatchEndpoints;

public static class ServicesExtensions
{
    public static IServiceCollection AddDispatchEndpoints(this IServiceCollection services)
    {
        services.AddMvcCore(options =>
        {
            options.Filters.Add(typeof(ValidatorActionFilter));
        });

        services.Scan(q => q.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
            .AddClasses(q => q.AssignableTo(typeof(IRequestHandler<>)))
            .AddClasses(q => q.AssignableTo(typeof(IRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        );

        services.AddSingleton<IDispatcher, Dispatcher>();

        return services;
    }
}