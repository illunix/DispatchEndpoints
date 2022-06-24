namespace DispatchEndpoints.Example.Endpoints.Customers;

[DispatchEndpoint(
    RequestMethod = RequestMethods.GET,
    ProducesResponseTypes = new[]
    { 
        StatusCodes.Ok,
        StatusCodes.BadRequest
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