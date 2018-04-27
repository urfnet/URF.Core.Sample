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
    public class CustomersController : ODataController
    {
        private readonly ICustomerService _customerService;
        private readonly IUnitOfWork _unitOfWork;

        public CustomersController(
            ICustomerService customerService,
            IUnitOfWork unitOfWork)
        {
            _customerService = customerService;
            _unitOfWork = unitOfWork;
        }

        // e.g. GET odata/Customers?$skip=2&$top=10
        [EnableQuery]
        public IQueryable<Customers> Get() => _customerService.Queryable();

        // e.g.  GET odata/Customers(37)
        public async Task<IActionResult> Get([FromODataUri] int key)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customer = await _customerService.FindAsync(key);

            if (customer == null)
                return NotFound();

            return Ok(customer);
        }

        // e.g. PUT odata/Customers(37)
        public async Task<IActionResult> Put([FromODataUri] string key, [FromBody] Customers customer)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != customer.CustomerId)
                return BadRequest();

            _customerService.Update(customer);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _customerService.ExistsAsync(key))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // e.g. PUT odata/Customers
        public async Task<IActionResult> Post([FromBody] Customers customer)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _customerService.Insert(customer);
            await _unitOfWork.SaveChangesAsync();

            return Created(customer);
        }

        // e.g. PATCH, MERGE odata/Customers(37)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IActionResult> Patch([FromODataUri] int key, [FromBody] Delta<Customers> customer)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await _customerService.FindAsync(key);
            if (entity == null)
                return NotFound();

            customer.Patch(entity);
            _customerService.Update(entity);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _customerService.ExistsAsync(key))
                    return NotFound();
                throw;
            }
            return Updated(entity);
        }

        // e.g. DELETE odata/Customers(37)
        public async Task<IActionResult> Delete([FromODataUri] int key)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _customerService.DeleteAsync(key);

            if (!result)
                return NotFound();

            await _unitOfWork.SaveChangesAsync();

            return StatusCode((int) HttpStatusCode.NoContent);
        }
    }
}