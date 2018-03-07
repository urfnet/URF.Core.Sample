# URF.Core.Sample #
**_<sup>URF.Core Sample Application | Unit-of-Work & Repository Framework | Official URF Team & [Trackable Entities](https://github.com/TrackableEntities) Team</sup>_**

[![Build Status](https://travis-ci.org/urfnet/URF.Core.svg?branch=master)](https://travis-ci.org/urfnet/URF.Core)
#### Docs: [comming soon](https://goo.gl/6zh9zp) | Subscribe URF Updates: [@lelong37](http://twitter.com/lelong37) | NuGet: [goo.gl/WEn7Jm](https://goo.gl/WEn7Jm) ###

#### URF sample and usage in ASP.NET Core Web API & OData *([goo.gl/URdYa1](https://goo.gl/URdYa1))*
```csharp
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
```