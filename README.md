# URF.Core.Sample #
**_<sup>URF.Core Sample Application | Unit-of-Work & Repository Framework | Official URF Team & [Trackable Entities](https://github.com/TrackableEntities) Team</sup>_**

[![Build Status](https://travis-ci.org/urfnet/URF.Core.svg?branch=master)](https://travis-ci.org/urfnet/URF.Core)
### Docs: [comming soon](https://goo.gl/6zh9zp) | Subscribe URF Updates: [@lelong37](http://twitter.com/lelong37) | NuGet: [goo.gl/WEn7Jm](https://goo.gl/WEn7Jm) ###

#### URF sample and usage in ASP.NET Core Web API & OData
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

    [EnableQuery]
    public IQueryable<Products> Get()
    {
        return _productService.Queryable();
    }

    public async Task<IActionResult> Get([FromODataUri] int key)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var products = await _productService.Queryable().SingleOrDefaultAsync(m => m.ProductId == key);

        if (products == null)
        {
            return NotFound();
        }

        return Ok(products);
    }

    public async Task<IActionResult> Put(int key, [FromBody] Products products)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (key != products.ProductId)
        {
            return BadRequest();
        }

        products.TrackingState = TrackingState.Modified;

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductsExists(key))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    public async Task<IActionResult> Post(Products products)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _productService.Insert(products);
        await _unitOfWork.SaveChangesAsync();

        return Created(products);
    }

    public async Task<IActionResult> Delete(int key)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var products = await _productService.Queryable().SingleOrDefaultAsync(m => m.ProductId == key);

        if (products == null)
        {
            return NotFound();
        }

        _productService.Delete(products);
        await _unitOfWork.SaveChangesAsync();

        return StatusCode((int) HttpStatusCode.NoContent);
    }

    private bool ProductsExists(int id)
    {
        return _productService.Queryable().Any(e => e.ProductId == id);
    }
}
```

#### URF sample and usage in ASP.NET Core Web API & OData
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
    services.AddTransient<IUnitOfWork, UnitOfWork>();
    services.AddTransient<ITrackableRepository<Products>, TrackableRepository<Products>>();
    services.AddTransient<IProductService, ProductService>();
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
#### Implementing Domain Logic with URF Service Pattern
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