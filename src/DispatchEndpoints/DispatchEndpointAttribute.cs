using System;

namespace DispatchEndpoints;

[AttributeUsage(AttributeTargets.Class)]
public class DispatchEndpointAttribute : Attribute
{
    public string? Controller { get; set; }
    public string? Route { get; set; }
    public RequestMethods RequestMethod { get; set; }
    public HttpStatusCodes[]? ProducesResponseTypes { get; set; }
    public bool Auth { get; set; }
    public string? Policy { get; set; }
}