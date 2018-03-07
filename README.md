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
#### URF sample and usage in ASP.NET Core Web API & OData (*https://goo.gl/ZF6JAH*)
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc()
        .AddJsonOptions(options =>
            options.SerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.All);

    services.AddOData();

    var connectionString = Configuration.GetConnectionString(nameof(NorthwindContext));
    services.AddDbContext<NorthwindContext>(options => options.UseSqlServer(connectionString));
    services.AddScoped<DbContext, NorthwindContext>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped<ITrackableRepository<Products>, TrackableRepository<Products>>();
    services.AddScoped<IProductService, ProductService>();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
        app.UseDeveloperExceptionPage();

    var oDataConventionModelBuilder = new ODataConventionModelBuilder(app.ApplicationServices);
    var entitySetConfiguration = oDataConventionModelBuilder.EntitySet<Products>(nameof(Products));        
    entitySetConfiguration.EntityType.HasKey(x => x.ProductId);
    entitySetConfiguration.EntityType.Ignore(x => x.Category);
    entitySetConfiguration.EntityType.Ignore(x => x.Supplier);
    entitySetConfiguration.EntityType.Ignore(x => x.OrderDetails);

    app.UseMvc(routeBuilder =>
        {
            routeBuilder.Select().Expand().Filter().OrderBy().MaxTop(1000).Count();
            routeBuilder.MapODataServiceRoute("ODataRoute", "odata", oDataConventionModelBuilder.GetEdmModel()); 
            routeBuilder.EnableDependencyInjection();
        }
    );
}
```
#### Implementing Domain Logic with URF Service Pattern (*https://goo.gl/n3Bbgc*)
```csharp
public class CustomerService : Service<Customer>, ICustomerService
{
    private readonly ITrackableRepository<Order> _ordeRepository;

    public CustomerService(
        ITrackableRepository<Customer> customerRepository,
        ITrackableRepository<Order> ordeRepository) : base(customerRepository)
    {
        _ordeRepository = ordeRepository;
    }

    public async Task<IEnumerable<Customer>> CustomersByCompany(string companyName)
    {
        return await Repository
            .Queryable()
            .Where(x => x.CompanyName.Contains(companyName))
            .ToListAsync();
    }

    public async Task<decimal> CustomerOrderTotalByYear(string customerId, int year)
    {
        return await Repository
            .Queryable()
            .Where(c => c.CustomerId == customerId)
            .SelectMany(c => c.Orders.Where(o => o.OrderDate != null && o.OrderDate.Value.Year == year))
            .SelectMany(c => c.OrderDetails)
            .Select(c => c.Quantity * c.UnitPrice)
            .SumAsync();
    }

    public async Task<IEnumerable<CustomerOrder>> GetCustomerOrder(string country)
    {
        var customers = Repository.Queryable();
        var orders = _ordeRepository.Queryable();

        var query = from c in customers
            join o in orders on new { a = c.CustomerId, b = c.Country }
                equals new { a = o.CustomerId, b = country }
            select new CustomerOrder
            {
                CustomerId = c.CustomerId,
                ContactName = c.ContactName,
                OrderId = o.OrderId,
                OrderDate = o.OrderDate
            };

        return await query.ToListAsync();
    }
}
```