namespace DispatchEndpoints.Example.Endpoints.Customers;

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
