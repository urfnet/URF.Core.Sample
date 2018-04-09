using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Northwind.Data.Models;

namespace Northwind.Data
{
    public class NorthwindContextFactory : IDesignTimeDbContextFactory<NorthwindContext>
    {
        public NorthwindContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<NorthwindContext>();
            optionsBuilder.UseSqlServer("Server=.;Database=Northwind;Trusted_Connection=True;");
            return new NorthwindContext(optionsBuilder.Options);
        }
    }
}