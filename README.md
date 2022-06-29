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
## Creating endpoint 
#### Query endpoint
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
        var customers = new List<Customer>();

        return await Task.FromResult(customers);
    }
}
```
#### Command endpoint
```csharp
[DispatchEndpoint(
    RequestMethod = HttpRequestMethods.POST,
    ProducesResponseTypes = new[]
    {
        HttpStatusCodes.Ok,
        HttpStatusCodes.BadRequest
    }
)]
public static partial class CreateCustomer
{
    public partial record Command;

    public static async Task Handler()
    {
        /* logic */
    }
}
```
### Creating Request With Fluent Validaton
```csharp
public partial record Query(string Id)
{
    public static void AddValidation(AbstractValidator<Query> v)
    {
        v.RuleFor(x => x.Id)
            .NotEmpty();
    }
}
```
## Generated Source
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
It's basically same for command/query
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

## Wiki
### Dispatch Endpoint Attribute
```csharp
[AttributeUsage(AttributeTargets.Class)]
public class DispatchEndpointAttribute : Attribute
{
    public string? Controller { get; set; } // Set controller name, if you leave it empty source generator will get the directory name where endpoint is located by namespace and use it as controller name
    public string? Route { get; set; } // Set route for endpoint  
    public HttpRequestMethods RequestMethod { get; set; } // Set http rquest method for method  
    public HttpStatusCodes[]? ProducesResponseTypes { get; set; } // Set produces response types status codes, first one is that what endpoint will return
    public bool Auth { get; set; } // Set auth to true
    public string? Policy { get; set; } // Set policy for auth
}
```
---
### Passing arguments to ``Query/Command`` property
```csharp
public partial record Command(string Name);
```
Then you need to pass that property as first parameter of your handler like this
```csharp
public static async Task Handler(Command request)
```
---
### Dependency injection
DI is automatic here, you just need to pass ``Query/Command`` as first parameter then you can pass as many you want interface parameters like this
```csharp
public static async Task Handler(Command request, ICustomersRepository repo, INotificationService service)
```
Source generator will create constructor for you in generated file
