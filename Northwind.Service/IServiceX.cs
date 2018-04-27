
using TrackableEntities.Common.Core;
using URF.Core.Abstractions.Services;

namespace Northwind.Service
{
  public interface IServiceX<TEntity> : IService<TEntity>, IRepositoryX<TEntity> where TEntity : class, ITrackable
  {

  }
}