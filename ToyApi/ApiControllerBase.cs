using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace ToyApi;

[ApiController]
public class ApiControllerBase : ControllerBase
{
    private IDispatcher? _dispatcher;

    protected IDispatcher Dispatcher
    {
        get
        {
            if (_dispatcher is null)
            {
                _dispatcher = HttpContext.RequestServices.GetService<IDispatcher>();
            }

            return _dispatcher!;
        }
    }
}