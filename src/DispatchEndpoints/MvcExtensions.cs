using DispatchEndpoints.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DispatchEndpoints;

public static class MvcExtensions
{
    public static IServiceCollection AddDispatchEndpoints(this IMvcBuilder builder)
    {
        builder.Services.AddMvcCore(options =>
        {
            options.Filters.Add(typeof(ValidatorActionFilter));
        });

        builder.Services.Scan(q => q.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
            .AddClasses(q => q.AssignableTo(typeof(IRequestHandler<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        );

        builder.Services.Scan(q => q.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
            .AddClasses(q => q.AssignableTo(typeof(IRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        );

        builder.Services.AddSingleton<IDispatcher, Dispatcher>();

        return builder.Services;
    }
}