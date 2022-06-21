﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ToyApi;

namespace ToyApi.Example.Endpoints.Customers 
{
    [Route("customers")]
    public partial class CustomersController : ApiControllerBase
    {
        [HttpGet()]
        public async Task<ActionResult<System.Collections.Generic.IEnumerable<ToyApi.Example.Endpoints.Customers.GetAll.Customer>>> GetAll([FromQuery] GetAll.Query request)
        {
            var query = await Dispatcher.Query(request);

            return Ok(query);
        }
    }
}
