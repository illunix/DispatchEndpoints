using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DispatchEndpoints;

namespace DispatchEndpoints.Example.Endpoints.Customers 
{
    [Route("customers")]
    public partial class CustomersController : ApiControllerBase
    {
        [HttpGET()]
        public async Task<ActionResult<System.Collections.Generic.IEnumerable<DispatchEndpoints.Example.Endpoints.Customers.GetAll.Customer>>> GetAll([FromQuery] GetAll.Query request)
        {
            var query = await Dispatcher.Query(request);

            return (query);
        }
    }
}
