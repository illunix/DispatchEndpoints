using ToyApi.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Configuration;

namespace ToyApi.Extensions;

public static class ServicesExtensions
{
    public static IMvcBuilder AddToyApi(this IMvcBuilder builder)
    {
        var services = builder.Services;

        services.Scan(q => q.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
            .AddClasses(q => q.AssignableTo(typeof(IRequestHandler<>)))
            .AddClasses(q => q.AssignableTo(typeof(IRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        );

        services.AddSingleton<IDispatcher, Dispatcher>();

        return builder;
    }
}