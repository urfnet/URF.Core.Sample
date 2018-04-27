using System;
using System.Linq.Expressions;
using Northwind.Data.Models;
using URF.Core.Abstractions.Services;

namespace Northwind.Service
{
  // Sample to extend ProductService, scoped to only ProductService vs. application wide
  public interface ICustomerService : IService<Customers>
  {
    // Example, adding synchronous Single method
    Customers Single(Expression<Func<Customers, bool>> predicate);
  }
}