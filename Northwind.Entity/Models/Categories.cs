using System;
using System.Collections.Generic;
using URF.Core.EF.Trackable;

namespace Northwind.Data.Models
{
    public partial class Categories : Entity
    {
        public Categories()
        {
            Products = new HashSet<Products>();
        }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public byte[] Picture { get; set; }

        public ICollection<Products> Products { get; set; }
    }
}
