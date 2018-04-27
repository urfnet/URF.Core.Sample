
using System.Threading;
using TrackableEntities.Common.Core;
using URF.Core.Abstractions.Trackable;

// Example: extending IRepository<TEntity> and/or ITrackableRepository<TEntity>, scope: application-wide across all IRepositoryX<TEntity>
public interface IRepositoryX<TEntity>: ITrackableRepository<TEntity> where TEntity : class, ITrackable
{
  // Example: adding synchronous Find, scope: application-wide
  TEntity Find(object[] keyValues, CancellationToken cancellationToken = default);
}