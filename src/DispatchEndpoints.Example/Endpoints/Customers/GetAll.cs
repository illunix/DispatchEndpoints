namespace DispatchEndpoints.Example.Endpoints.Customers;

[DispatchEndpoint(
    RequestMethod = RequestMethods.Get,
    StatusCode = StatusCodes.Ok
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
