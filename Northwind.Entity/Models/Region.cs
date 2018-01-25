using System;
using System.Collections.Generic;

namespace Northwind.Data.Models
{
    public partial class Region
    {
        public Region()
        {
            Territories = new HashSet<Territories>();
        }

        public int RegionId { get; set; }
        public string RegionDescription { get; set; }

        public ICollection<Territories> Territories { get; set; }
    }
}
