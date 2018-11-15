using URF.Core.Abstractions.Trackable;
using URF.Core.Services;

using Northwind.Data.Models;

public class CategoriesService : Service<Categories>, ICategoriesService
{
    public CategoriesService(ITrackableRepository<Categories> repository) : base(repository)
    {
    }
}
