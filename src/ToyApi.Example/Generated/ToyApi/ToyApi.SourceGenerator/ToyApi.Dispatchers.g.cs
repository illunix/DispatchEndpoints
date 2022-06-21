namespace ToyApi.Example.Endpoints.Customers
{
    public partial class GetAll 
    {
        public partial record Query : IRequest<System.Collections.Generic.IEnumerable<ToyApi.Example.Endpoints.Customers.GetAll.Customer>> { }
        
        private class QueryHandlerCore : IRequestHandler<GetAll.Query, System.Collections.Generic.IEnumerable<ToyApi.Example.Endpoints.Customers.GetAll.Customer>>
        {
            
            public async Task<System.Collections.Generic.IEnumerable<ToyApi.Example.Endpoints.Customers.GetAll.Customer>> Handle(Query request, CancellationToken cancellationToken) 
            {
                return await Handler();
            }
        }
    }
}