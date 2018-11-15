using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using URF.Core.Abstractions;

using Northwind.Data.Models;

namespace Northwind.Api.OData
{
    public class CategoriesController : ODataController
    {
        private readonly ICategoriesService _categoriesService;
        private readonly IUnitOfWork _unitOfWork;

        public CategoriesController(
            ICategoriesService categoriesService,
            IUnitOfWork unitOfWork
        )
        {
            _categoriesService = categoriesService;
            _unitOfWork = unitOfWork;
        }

        // GET: api/Categories
        [EnableQuery]
        public IEnumerable<Categories> Get()
        {
            return _categoriesService.Queryable();
        }

        public async Task<IActionResult> Get([FromODataUri] int key)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var categories = await _categoriesService.FindAsync(key);

            if (categories == null)
                return NotFound();

            return Ok(categories);
        }

        public async Task<IActionResult> Put([FromODataUri] int key, [FromBody] Categories categories)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (key != categories.CategoryId)
                return BadRequest();
            
            _categoriesService.Update(categories);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _categoriesService.ExistsAsync(key))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        public async Task<IActionResult> Post([FromBody] Categories categories)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _categoriesService.Insert(categories);

            await _unitOfWork.SaveChangesAsync();
            return CreatedAtAction("Get", new { key = categories.CategoryId }, categories);
        }

        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IActionResult> Patch([FromODataUri] int key, [FromBody] Delta<Categories> categories)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await _categoriesService.FindAsync(key);
            if (entity == null)
                return NotFound();

            categories.Patch(entity);
            _categoriesService.Update(entity);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _categoriesService.ExistsAsync(key))
                    return NotFound();
                throw;
            }

            return Updated(entity);
        }

        public async Task<IActionResult> Delete([FromODataUri] int key)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _categoriesService.DeleteAsync(key);

            if (!result)
                return NotFound();

            await _unitOfWork.SaveChangesAsync();

            return Ok();
        }
    }
}
