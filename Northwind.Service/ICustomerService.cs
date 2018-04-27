using System;
using System.Linq.Expressions;
using Northwind.Data.Models;
using URF.Core.Abstractions.Services;

namespace Northwind.Service
{
  // Example: extending IService<TEntity> and/or ITrackableRepository<TEntity>, scope: ICustomerService
  public interface ICustomerService : IService<Customers>
  {
    // Example: adding synchronous Single method, scope: ICustomerService
    Customers Single(Expression<Func<Customers, bool>> predicate);
  }
}