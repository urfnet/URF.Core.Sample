#region

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.Data.Models;
using Northwind.Service;
using TrackableEntities.Common.Core;
using Urf.Core.Abstractions;

#endregion

namespace Northwind.Api.OData
{
    public class ProductsController : ODataController
    {
        private readonly IProductService _productService;
        private readonly IUnitOfWork _unitOfWork;

        public ProductsController(
            IProductService productService,
            IUnitOfWork unitOfWork)
        {
            _productService = productService;
            _unitOfWork = unitOfWork;
        }

        // e.g. GET odata/Products?$skip=2&$top=10
        [EnableQuery]
        public IQueryable<Products> Get() => _productService.Queryable();

        // e.g.  GET odata/Products(37)
        public async Task<IActionResult> Get([FromODataUri] int key)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _productService.FindAsync(key);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        // e.g. PUT odata/Products(37)
        public async Task<IActionResult> Put([FromODataUri] int key, [FromBody] Products products)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != products.ProductId)
                return BadRequest();

            _productService.Update(products);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _productService.ExistsAsync(key))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // e.g. PUT odata/Products
        public async Task<IActionResult> Post([FromBody] Products products)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _productService.Insert(products);
            await _unitOfWork.SaveChangesAsync();

            return Created(products);
        }

        // e.g. PATCH, MERGE odata/Products(37)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IActionResult> Patch([FromODataUri] int key, [FromBody] Delta<Products> product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await _productService.FindAsync(key);
            if (entity == null)
                return NotFound();

            product.Patch(entity);
            _productService.Update(entity);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _productService.ExistsAsync(key))
                    return NotFound();
                throw;
            }
            return Updated(entity);
        }

        // e.g. DELETE odata/Products(37)
        public async Task<IActionResult> Delete([FromODataUri] int key)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _productService.DeleteAsync(key);

            if (!result)
                return NotFound();

            await _unitOfWork.SaveChangesAsync();

            return StatusCode((int) HttpStatusCode.NoContent);
        }
    }
}