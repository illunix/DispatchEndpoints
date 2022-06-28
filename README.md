# DispatchEndpoints
[![NuGet](https://img.shields.io/nuget/dt/DispatchEndpoints.svg)](https://www.nuget.org/packages/DispatchEndpoints) 
[![NuGet](https://img.shields.io/nuget/vpre/DispatchEndpoints.svg)](https://www.nuget.org/packages/DispatchEndpoints)

DispatchEndpoints it's a library what provide source generator what generate controllers/actions based on your class where you use ``DispatchEndpoint`` attribute.

## Installation

```
Install-Package DispatchEndpoints
```

## Getting started
Register service
```csharp
services.AddControllers().AddDispatchEndpoints()
```
### Example of creating endpoint 
```csharp
[DispatchEndpoint(
    RequestMethod = HttpRequestMethods.GET,
    ProducesResponseTypes = new[]
    {
        HttpStatusCodes.Ok,
        HttpStatusCodes.BadRequest
    }
)]
public static partial class GetAll
{
    public partial record Query;

    public record Customer(string Name);

    public static async Task<IEnumerable<Customer>> Handler()
    {
        var customers = new Customer[]
        {
            new("Google"),
            new("Amazon"),
            new("Facebook")
        };

        return await Task.FromResult(customers);
    }
}
```
### Generated Controller
```csharp
[Route("customers")]
public partial class CustomersController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status200OK)]
    [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<System.Collections.Generic.IEnumerable<DispatchEndpoints.Example.Endpoints.Customers.GetAll.Customer>>> GetAll([FromQuery] GetAll.Query request)
    {
        var query = await Dispatcher.Query(request);

        return Ok(query);
    }
}    
```
### Generated Dispatcher
```csharp
public partial class GetAll 
{
    public partial record Query : IRequest<IEnumerable<Customer>> { }
        
    private class QueryHandlerCore : IRequestHandler<GetAll.Query, IEnumerable<Customer>>
    {
        public async Task<IEnumerable<Customer>> Handle(Query request, CancellationToken cancellationToken) 
        {
            return await Handler();
        }
    }
}
```

---

## Wiki
### Dispatch Endpoint Attribute
```csharp
[AttributeUsage(AttributeTargets.Class)]
public class DispatchEndpointAttribute : Attribute
{
    public string? Controller { get; set; } // Set controller name, if you leave it empty source generator will get the directory name where endpoint is located by namespace
    public string? Route { get; set; } // Set route for endpoint  
    public HttpRequestMethods RequestMethod { get; set; } // Set http rquest method for method  
    public HttpStatusCodes[]? ProducesResponseTypes { get; set; } // Set produces response types status codes, first one is that what endpoint will return
    public bool Auth { get; set; } // Set auth to true
    public string? Policy { get; set; } // Set policy for auth
}
```
