
using System.Threading;
using TrackableEntities.Common.Core;
using URF.Core.Abstractions.Trackable;

public interface IRepositoryX<TEntity>: ITrackableRepository<TEntity> where TEntity : class, ITrackable
{
  TEntity Find(object[] keyValues, CancellationToken cancellationToken = default);
}