using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ToyApi.Interfaces;

public interface IRequestHandler<in TRequest> where TRequest : class, IRequest
{
    Task Handle(
        TRequest request,
        CancellationToken cancellationToken = default
    );
}

public interface IRequestHandler<in TRequest, TResult> where TRequest : class, IRequest<TResult>
{
    Task<TResult> Handle(
        TRequest query,
        CancellationToken cancellationToken = default
    );
}