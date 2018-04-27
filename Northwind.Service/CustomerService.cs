using System.Threading;
using Northwind.Data.Models;
using URF.Core.Abstractions.Trackable;
using URF.Core.Services;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System;

// Sample to extend ProductService, scoped to only ProductService vs. application wide
namespace Northwind.Service
{
  public class CustomerService : Service<Customers>, ICustomerService
  {
    public CustomerService(IRepositoryX<Customers> repository) : base(repository)
    {
    }

    // Example, adding synchronous Single method
    public Customers Single(Expression<Func<Customers, bool>> predicate)
    {
      return this.Repository.Queryable().Single(predicate);
    }
  }
}
