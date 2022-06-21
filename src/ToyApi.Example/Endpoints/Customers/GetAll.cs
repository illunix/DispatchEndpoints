namespace ToyApi.Example.Endpoints.Customers;

[DispatchEndpoint(
    RequestMethod = RequestMethods.Get,
    StatusCode = StatusCodes.Ok
)]
public static partial class GetAll
{
    public sealed partial record Query;

    public sealed record Customer(string Name);

    public static async Task<IEnumerable<Customer>> Handler()
    {
        var customers = new Customer[]
        {
            new("Google"),
            new("Amazon"),
            new("Facebook")
        };

        return customers;
    }
}