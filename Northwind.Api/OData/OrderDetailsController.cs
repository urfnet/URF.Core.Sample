using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.Data.Models;
using Northwind.Service;
using URF.Core.Abstractions;

namespace Northwind.Api.OData
{
    public class OrderDetailsController : ODataController
    {
        private readonly IOrderDetailService _orderDetailService;
        private readonly IUnitOfWork _unitOfWork;

        public OrderDetailsController(
            IOrderDetailService orderDetailService,
            IUnitOfWork unitOfWork)
        {
            _orderDetailService = orderDetailService;
            _unitOfWork = unitOfWork;
        }

        // e.g. GET odata/Products?$skip=2&$top=10
        [EnableQuery]
        public IQueryable<OrderDetails> Get()
        {
            return _orderDetailService.Queryable();
        }

        // e.g.  GET odata/Products(37)
        public async Task<IActionResult> Get([FromODataUri] int key)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _orderDetailService.FindAsync(key);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        // e.g. PUT odata/Products(37)
        public async Task<IActionResult> Put([FromODataUri] int key, [FromBody] OrderDetails orderDetail)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != orderDetail.ProductId)
                return BadRequest();

            _orderDetailService.Update(orderDetail);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _orderDetailService.ExistsAsync(key))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // e.g. PUT odata/Products
        public async Task<IActionResult> Post([FromBody] OrderDetails orderDetails)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _orderDetailService.Insert(orderDetails);
            await _unitOfWork.SaveChangesAsync();

            return Created(orderDetails);
        }

        // e.g. PATCH, MERGE odata/Products(37)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IActionResult> Patch([FromODataUri] int key, [FromBody] Delta<OrderDetails> orderDetail)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await _orderDetailService.FindAsync(key);
            if (entity == null)
                return NotFound();

            orderDetail.Patch(entity);
            _orderDetailService.Update(entity);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _orderDetailService.ExistsAsync(key))
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

            var result = await _orderDetailService.DeleteAsync(key);

            if (!result)
                return NotFound();

            await _unitOfWork.SaveChangesAsync();

            return StatusCode((int) HttpStatusCode.NoContent);
        }
    }
}