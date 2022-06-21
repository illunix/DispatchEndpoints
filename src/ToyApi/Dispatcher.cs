using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ToyApi;

public interface IDispatcher
{
    Task Send<TResult>(
        TResult request,
        CancellationToken cancellationToken = default
    ) where TResult : class, IRequest;

    Task<TResult> Query<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default
    );
}

public sealed class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Send<TResult>(
        TResult request,
        CancellationToken cancellationToken
    ) where TResult : class, IRequest
    {
        using var scope = _serviceProvider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<TResult>>();

        await handler.Handle(
            request,
            cancellationToken
        );
    }

    public async Task<TResult> Query<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default
    )
    {
        using var scope = _serviceProvider.CreateScope();

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(
            request.GetType(),
            typeof(TResult)
        );

        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

        return await (Task<TResult>)handlerType
            .GetMethod(nameof(IRequestHandler<IRequest<TResult>, TResult>.Handle))?
            .Invoke(
                handler,
                new object[] { request, cancellationToken }
            )!;
    }
}