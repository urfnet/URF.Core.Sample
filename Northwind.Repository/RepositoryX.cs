using System;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using TrackableEntities.Common.Core;
using URF.Core.EF.Trackable;

// Sample to extend Repository<TEntity>, scopded application wide
namespace Northwind.Repository
{
  public class RepositoryX<TEntity> : TrackableRepository<TEntity>, IRepositoryX<TEntity> where TEntity : class, ITrackable
  {
    public RepositoryX(DbContext context) : base(context)
    {
    }

    // Example, adding synchronous Find application wide for all repositories
    public TEntity Find(object[] keyValues, CancellationToken cancellationToken = default)
    {
      return this.Context.Find<TEntity>(keyValues) as TEntity;
    }
  }
}
