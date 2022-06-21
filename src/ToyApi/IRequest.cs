using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyApi;

public interface IRequest
{
}

public interface IRequest<T> : IRequest
{
}