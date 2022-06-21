using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Configuration;
using ToyApi.Filters;

namespace ToyApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddToyApi(this IServiceCollection services)
    {
        services.AddControllers(options =>
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