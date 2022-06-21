namespace ToyApi.Example.Endpoints.Customers;

[DispatchEndpoint(
    RequestMethod = RequestMethods.Get,
    StatusCode = StatusCodes.Ok
)]
public static partial class GetAll
{
    public sealed partial record Query;

    public static async Task Handler()
    {

    }
}